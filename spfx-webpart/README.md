# Heather Chat — SharePoint SPFx Web Part

A SharePoint Framework (SPFx) web part that embeds the **Heather** AI HR assistant
directly into any SharePoint Online page. It calls the same API that the
HeatherDemoApp Razor Pages UI uses, proxied through a `/api/chat` endpoint on
the ASP.NET app to avoid CORS issues.

---

## Architecture

```
┌─────────────────────────┐
│  SharePoint Online Page │
│  ┌───────────────────┐  │
│  │  SPFx Web Part    │  │     POST /api/chat
│  │  (React + Fluent) │──┼──────────────────────┐
│  └───────────────────┘  │                      │
└─────────────────────────┘                      ▼
                                   ┌──────────────────────────┐
                                   │  HeatherDemoApp          │
                                   │  (ASP.NET on Azure)      │
                                   │  heather-demo-chat       │
                                   │  .azurewebsites.net      │
                                   └──────────┬───────────────┘
                                              │ POST
                                              ▼
                                   ┌──────────────────────────┐
                                   │  Azure Function          │
                                   │  Chat API                │
                                   │  playwright-scraper-func │
                                   │  .azurewebsites.net      │
                                   └──────────────────────────┘
```

The SPFx web part talks **only** to the HeatherDemoApp `/api/chat` endpoint
(which has CORS configured for `*.sharepoint.com`). The HeatherDemoApp then
forwards the request server-side to the Azure Function — no direct
cross-origin call to the Function is needed.

---

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| **Node.js** | 18.x LTS | https://nodejs.org |
| **npm** | 9+ | Comes with Node.js |
| **Gulp CLI** | 4.x | `npm install -g gulp-cli` |
| **Yeoman** | 4.x | `npm install -g yo` |
| **SPFx Generator** | 1.20.x | `npm install -g @microsoft/generator-sharepoint` |

> **Note:** SPFx 1.20 requires **Node.js 18.x** (not 20+). Use `nvm` if you
> need to manage multiple Node versions.

You also need:
- A **SharePoint Online** tenant (Microsoft 365)
- An **App Catalog** site (tenant-level or site-collection level)
- Permission to deploy SPFx solutions (SharePoint Admin or Site Collection Admin)

---

## Quick Start

### 1. Install dependencies

```bash
cd spfx-webpart
npm install
```

### 2. Update the API URL (if needed)

The default API base URL is configured in the web part manifest:

**File:** `src/webparts/heatherChat/HeatherChatWebPart.manifest.json`

```json
"properties": {
  "apiBaseUrl": "https://heather-demo-chat.azurewebsites.net"
}
```

Change this if your HeatherDemoApp is deployed to a different URL. This can
also be changed per-instance via the web part property pane in SharePoint.

### 3. Test locally (optional)

Edit `config/serve.json` and set `initialPage` to your SharePoint site:

```json
{
  "initialPage": "https://yourtenant.sharepoint.com/sites/yoursite/_layouts/workbench.aspx"
}
```

Then run:

```bash
gulp serve
```

This launches the SPFx local workbench and opens the SharePoint workbench
where you can add and test the web part.

### 4. Build for production

```bash
gulp bundle --ship
gulp package-solution --ship
```

This produces:

```
sharepoint/solution/heather-chat-webpart.sppkg
```

### 5. Deploy to SharePoint

1. Go to your **SharePoint Admin Center** → **More features** → **Apps** → **App Catalog**
   - Or go directly to: `https://yourtenant.sharepoint.com/sites/appcatalog`
2. Click **Apps for SharePoint** in the left nav
3. Click **Upload** and select `sharepoint/solution/heather-chat-webpart.sppkg`
4. In the trust dialog, check **"Make this solution available to all sites"** and click **Deploy**

### 6. Add the web part to a page

1. Navigate to any modern SharePoint page
2. Click **Edit** (pencil icon)
3. Click **+** to add a web part
4. Search for **"Heather Chat"**
5. Click to add it to the page
6. (Optional) Click the pencil icon on the web part to open the property pane
   and change the API Base URL if needed
7. Click **Publish**

---

## Configuration

### Web Part Properties

| Property | Default | Description |
|----------|---------|-------------|
| `apiBaseUrl` | `https://heather-demo-chat.azurewebsites.net` | The base URL of the HeatherDemoApp. The web part appends `/api/chat` to this. |

### CORS (already configured)

The HeatherDemoApp's `Program.cs` includes a CORS policy named `SharePointSPFx`
that allows requests from:

- `https://*.sharepoint.com`
- `https://*.sharepoint.us`
- `https://localhost:4321` (SPFx local workbench)

The `/api/chat` endpoint is decorated with `.RequireCors("SharePointSPFx")`.

If you need to add additional origins (e.g., a custom domain), edit the
CORS policy in `Program.cs`.

---

## Project Structure

```
spfx-webpart/
├── config/
│   ├── config.json                  # Build config (bundles, resources)
│   ├── deploy-azure-storage.json    # CDN deployment config
│   ├── package-solution.json        # Solution packaging config
│   ├── serve.json                   # Local dev server config
│   └── write-manifests.json         # CDN manifest config
├── src/
│   ├── index.ts                     # Entry point
│   └── webparts/
│       └── heatherChat/
│           ├── HeatherChatWebPart.manifest.json   # Web part manifest
│           ├── HeatherChatWebPart.ts              # Web part class
│           ├── components/
│           │   ├── HeatherChat.tsx                 # Main React component
│           │   └── HeatherChat.module.scss         # Scoped styles
│           └── loc/
│               ├── en-us.js                       # English strings
│               └── mystrings.d.ts                 # String type defs
├── .gitignore
├── .yo-rc.json
├── gulpfile.js
├── package.json
├── tsconfig.json
└── README.md                        # This file
```

---

## How It Works

1. User types a question in the SharePoint web part
2. The React component sends a `POST` request to
   `https://heather-demo-chat.azurewebsites.net/api/chat`
   with body `{ "message": "user's question" }`
3. The HeatherDemoApp receives this, forwards it to the Azure Function chat API
4. The Azure Function processes the question against the knowledge base
5. The response flows back: Azure Function → HeatherDemoApp → SPFx web part
6. The web part renders Heather's response with basic markdown formatting

---

## Redeploy the ASP.NET App

After adding the `/api/chat` endpoint and CORS policy, you need to redeploy
the HeatherDemoApp:

```powershell
# From the HeatherDemoApp root directory
dotnet publish -c Release -o ./publish
az webapp deploy --resource-group RG-Marc.Merritt --name heather-demo-chat --src-path ./publish --type zip
```

Or use whichever deployment script/method you normally use (e.g., `deploy_v4.ps1`).

---

## Troubleshooting

### "Access to fetch has been blocked by CORS policy"
- Ensure the HeatherDemoApp has been redeployed with the CORS policy in `Program.cs`
- Check that the SharePoint origin matches the allowed origins pattern

### Web part doesn't appear in the web part gallery
- Ensure you deployed the `.sppkg` to the App Catalog
- Check that you clicked "Deploy" in the trust dialog
- If using a site-collection app catalog, make sure you're on the correct site

### "Heather is thinking..." never resolves
- Open browser DevTools (F12) → Network tab → check the `/api/chat` request
- Verify the API base URL is correct in the web part properties
- Check that the HeatherDemoApp is running and accessible

### Local workbench issues
- Ensure you're running Node.js 18.x (`node -v`)
- Accept the localhost SSL certificate when prompted
- Try `gulp clean && gulp serve` to rebuild from scratch
