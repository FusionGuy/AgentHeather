using System.Text.RegularExpressions;
using HeatherDemoApp.Models;

namespace HeatherDemoApp.Services;

public record TextChunk(string DocId, string Title, string FileName, string Text);

public interface IRetrievalService
{
    Task InitializeAsync(List<PdfDocument> docs);
    Task<List<TextChunk>> GetTopChunksAsync(string query, int k = 5);
}

public class TfIdfRetrievalService : IRetrievalService
{
    private readonly object _lock = new();
    private bool _initialized;
    private readonly List<TextChunk> _chunks = new();
    private readonly Dictionary<string, int> _df = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","a","an","and","or","of","to","in","for","on","at","by","with","from","is","are","was","were","be","been","it","that","this","as","we","you","your","our","their"
    };

    public async Task InitializeAsync(List<PdfDocument> docs)
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            foreach (var d in docs)
            {
                foreach (var c in ChunkText(d.Content, 700, 150))
                {
                    _chunks.Add(new TextChunk(d.Id, d.Title, d.FileName, c));
                }
            }
            // Build document frequency for terms across chunks
            foreach (var chunk in _chunks)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in Tokenize(chunk.Text))
                {
                    if (!seen.Add(t)) continue;
                    if (_df.TryGetValue(t, out var cnt)) _df[t] = cnt + 1; else _df[t] = 1;
                }
            }
            _initialized = true;
        }
        await Task.CompletedTask;
    }

    public Task<List<TextChunk>> GetTopChunksAsync(string query, int k = 5)
    {
        if (!_initialized) return Task.FromResult(new List<TextChunk>());
        var qTokens = Tokenize(query);
        var qVec = BuildTfidf(qTokens);
        var scored = new List<(double score, TextChunk chunk)>();
        foreach (var ch in _chunks)
        {
            var cVec = BuildTfidf(Tokenize(ch.Text));
            var score = Cosine(qVec, cVec);
            if (score > 0) scored.Add((score, ch));
        }
        var top = scored
            .OrderByDescending(x => x.score)
            .Take(k)
            .Select(x => x.chunk)
            .ToList();
        return Task.FromResult(top);
    }

    private IEnumerable<string> Tokenize(string text)
    {
        foreach (Match m in Regex.Matches(text.ToLowerInvariant(), "[a-z0-9]{2,}"))
        {
            var tok = m.Value;
            if (_stop.Contains(tok)) continue;
            yield return tok;
        }
    }

    private Dictionary<string, double> BuildTfidf(IEnumerable<string> tokens)
    {
        var tf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int count = 0;
        foreach (var t in tokens)
        {
            count++;
            if (tf.TryGetValue(t, out var c)) tf[t] = c + 1; else tf[t] = 1;
        }
        var vec = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (count == 0) return vec;
        double N = Math.Max(_chunks.Count, 1);
        foreach (var kv in tf)
        {
            var term = kv.Key;
            var tfNorm = (double)kv.Value / count;
            _df.TryGetValue(term, out var df);
            var idf = Math.Log((N + 1) / (df + 1)) + 1.0; // smoothed IDF
            vec[term] = tfNorm * idf;
        }
        return vec;
    }

    private static double Cosine(Dictionary<string, double> a, Dictionary<string, double> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        double dot = 0, na = 0, nb = 0;
        foreach (var kv in a)
        {
            na += kv.Value * kv.Value;
            if (b.TryGetValue(kv.Key, out var bv)) dot += kv.Value * bv;
        }
        foreach (var kv in b) nb += kv.Value * kv.Value;
        if (na == 0 || nb == 0) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    private static IEnumerable<string> ChunkText(string text, int size, int overlap)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        var len = text.Length;
        int start = 0;
        while (start < len)
        {
            int end = Math.Min(start + size, len);
            yield return text.Substring(start, end - start);
            if (end == len) break;
            start = Math.Max(0, end - overlap);
        }
    }
}
