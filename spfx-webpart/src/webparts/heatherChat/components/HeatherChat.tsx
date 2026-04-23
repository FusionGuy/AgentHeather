import * as React from 'react';
import {
  TextField,
  PrimaryButton,
  DefaultButton,
  Stack,
  Spinner,
  SpinnerSize,
  Text,
  Icon,
  MessageBar,
  MessageBarType
} from '@fluentui/react';
import { WebPartContext } from '@microsoft/sp-webpart-base';
import styles from './HeatherChat.module.scss';

// ── Heather avatar as an inline SVG data URI ─────────────────────────
// This is the same avatar used in the ASP.NET Razor Pages app so
// the SharePoint web part looks visually identical.
const HEATHER_AVATAR = `data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 200' width='200' height='200'%3E%3Ccircle cx='100' cy='100' r='96' fill='%23E8F5E9' stroke='%234CAF50' stroke-width='3'/%3E%3Cellipse cx='100' cy='85' rx='52' ry='55' fill='%238D6E63'/%3E%3Cellipse cx='55' cy='100' rx='18' ry='35' fill='%238D6E63'/%3E%3Cellipse cx='145' cy='100' rx='18' ry='35' fill='%238D6E63'/%3E%3Cpath d='M 55 75 Q 70 55 100 60 Q 130 55 145 75' fill='%23795548' opacity='0.6'/%3E%3Cellipse cx='100' cy='95' rx='40' ry='45' fill='%23FFDAB9'/%3E%3Cpath d='M 60 72 Q 75 55 100 58 Q 125 55 140 72 Q 130 62 100 65 Q 70 62 60 72' fill='%238D6E63'/%3E%3Cellipse cx='82' cy='90' rx='6' ry='7' fill='white'/%3E%3Cellipse cx='118' cy='90' rx='6' ry='7' fill='white'/%3E%3Ccircle cx='83' cy='91' r='3.5' fill='%235D4037'/%3E%3Ccircle cx='119' cy='91' r='3.5' fill='%235D4037'/%3E%3Ccircle cx='84.5' cy='89.5' r='1.2' fill='white'/%3E%3Ccircle cx='120.5' cy='89.5' r='1.2' fill='white'/%3E%3Cpath d='M 74 82 Q 82 78 90 81' stroke='%235D4037' stroke-width='2' fill='none' stroke-linecap='round'/%3E%3Cpath d='M 110 81 Q 118 78 126 82' stroke='%235D4037' stroke-width='2' fill='none' stroke-linecap='round'/%3E%3Cpath d='M 100 95 Q 97 102 100 104 Q 103 102 100 95' fill='%23E8C4A0' opacity='0.7'/%3E%3Cpath d='M 85 110 Q 100 122 115 110' stroke='%23D4796A' stroke-width='2.5' fill='none' stroke-linecap='round'/%3E%3Cellipse cx='72' cy='105' rx='8' ry='5' fill='%23FFB3B3' opacity='0.5'/%3E%3Cellipse cx='128' cy='105' rx='8' ry='5' fill='%23FFB3B3' opacity='0.5'/%3E%3C/svg%3E`;

export interface IHeatherChatProps {
  apiBaseUrl: string;
  context: WebPartContext;
}

interface IChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

interface IHeatherChatState {
  messages: IChatMessage[];
  inputValue: string;
  isLoading: boolean;
  error: string | null;
}

export class HeatherChat extends React.Component<IHeatherChatProps, IHeatherChatState> {
  private chatEndRef: React.RefObject<HTMLDivElement>;

  constructor(props: IHeatherChatProps) {
    super(props);
    this.chatEndRef = React.createRef<HTMLDivElement>();
    this.state = {
      messages: [],
      inputValue: '',
      isLoading: false,
      error: null
    };
  }

  public componentDidUpdate(_prevProps: IHeatherChatProps, prevState: IHeatherChatState): void {
    if (prevState.messages.length !== this.state.messages.length) {
      this._scrollToBottom();
    }
  }

  private _scrollToBottom(): void {
    if (this.chatEndRef.current) {
      this.chatEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }

  private _handleInputChange = (_ev: React.FormEvent<HTMLInputElement | HTMLTextAreaElement>, newValue?: string): void => {
    this.setState({ inputValue: newValue || '' });
  }

  private _handleKeyDown = (ev: React.KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>): void => {
    if (ev.key === 'Enter' && !ev.shiftKey) {
      ev.preventDefault();
      this._sendMessage();
    }
  }

  private _sendMessage = async (): Promise<void> => {
    const question = this.state.inputValue.trim();
    if (!question || this.state.isLoading) return;

    // Add user message and clear input
    this.setState(prevState => ({
      messages: [...prevState.messages, { role: 'user', content: question }],
      inputValue: '',
      isLoading: true,
      error: null
    }));

    try {
      // Call the /api/chat proxy endpoint on the HeatherDemoApp
      const apiUrl = `${this.props.apiBaseUrl.replace(/\/+$/, '')}/api/chat`;
      const response = await fetch(apiUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify({ message: question })
      });

      if (!response.ok) {
        throw new Error(`Server returned ${response.status}: ${response.statusText}`);
      }

      const data = await response.json();
      const answer: string = data.response || 'Sorry, I received an empty response.';

      this.setState(prevState => ({
        messages: [...prevState.messages, { role: 'assistant', content: answer }],
        isLoading: false
      }));
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error';
      this.setState(prevState => ({
        messages: [
          ...prevState.messages,
          { role: 'assistant', content: 'Sorry, I encountered an error. Please try again.' }
        ],
        isLoading: false,
        error: `API Error: ${errorMessage}`
      }));
    }
  }

  private _clearChat = (): void => {
    this.setState({
      messages: [],
      inputValue: '',
      isLoading: false,
      error: null
    });
  }

  /**
   * Convert basic markdown to HTML for rendering assistant responses.
   * Handles bold, italic, line breaks, bullet lists, and numbered lists.
   */
  private _renderMarkdown(text: string): string {
    let html = text
      // Escape HTML entities
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      // Bold: **text** or __text__
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/__(.*?)__/g, '<strong>$1</strong>')
      // Italic: *text* or _text_
      .replace(/\*(.*?)\*/g, '<em>$1</em>')
      .replace(/_(.*?)_/g, '<em>$1</em>')
      // Inline code: `code`
      .replace(/`(.*?)`/g, '<code>$1</code>')
      // Headings: ### text
      .replace(/^### (.*$)/gm, '<h4>$1</h4>')
      .replace(/^## (.*$)/gm, '<h3>$1</h3>')
      .replace(/^# (.*$)/gm, '<h2>$1</h2>')
      // Horizontal rule
      .replace(/^---$/gm, '<hr/>')
      // Bullet lists: - item or * item
      .replace(/^\s*[-*]\s+(.*$)/gm, '<li>$1</li>')
      // Numbered lists: 1. item
      .replace(/^\s*\d+\.\s+(.*$)/gm, '<li>$1</li>')
      // Line breaks
      .replace(/\n/g, '<br/>');

    // Wrap consecutive <li> elements in <ul>
    html = html.replace(/((?:<li>.*?<\/li><br\/>?)+)/g, (match) => {
      const cleaned = match.replace(/<br\/?>/g, '');
      return `<ul>${cleaned}</ul>`;
    });

    return html;
  }

  private _renderMessage(msg: IChatMessage, index: number): React.ReactElement {
    if (msg.role === 'user') {
      return (
        <div key={index} className={styles.messageRow + ' ' + styles.userRow}>
          <div className={styles.messageBubble + ' ' + styles.userBubble}>
            <div className={styles.messageLabel}>
              <Icon iconName="Contact" className={styles.labelIcon} /> You
            </div>
            <div className={styles.messageText}>{msg.content}</div>
          </div>
        </div>
      );
    }

    return (
      <div key={index} className={styles.messageRow + ' ' + styles.assistantRow}>
        <img
          src={HEATHER_AVATAR}
          alt="Heather"
          className={styles.avatar}
        />
        <div className={styles.messageBubble + ' ' + styles.assistantBubble}>
          <div className={styles.messageLabel + ' ' + styles.assistantLabel}>
            Heather
          </div>
          <div
            className={styles.messageText}
            dangerouslySetInnerHTML={{ __html: this._renderMarkdown(msg.content) }}
          />
        </div>
      </div>
    );
  }

  public render(): React.ReactElement<IHeatherChatProps> {
    const { messages, inputValue, isLoading, error } = this.state;

    return (
      <div className={styles.heatherChat}>
        {/* Header */}
        <Stack horizontal verticalAlign="center" tokens={{ childrenGap: 12 }} className={styles.header}>
          <img
            src={HEATHER_AVATAR}
            alt="Heather"
            className={styles.headerAvatar}
          />
          <Stack>
            <Text variant="xLarge" className={styles.headerTitle}>Chat with Heather</Text>
            <Text variant="small" className={styles.headerSubtitle}>
              Your HR Policy Assistant 💼
            </Text>
          </Stack>
        </Stack>

        {/* Error banner */}
        {error && (
          <MessageBar
            messageBarType={MessageBarType.warning}
            onDismiss={() => this.setState({ error: null })}
            dismissButtonAriaLabel="Close"
            className={styles.errorBar}
          >
            {error}
          </MessageBar>
        )}

        {/* Chat messages area */}
        <div className={styles.chatBody}>
          {messages.length === 0 && (
            <div className={styles.emptyState}>
              <img
                src={HEATHER_AVATAR}
                alt="Heather"
                className={styles.emptyAvatar}
              />
              <Text variant="mediumPlus" className={styles.emptyText}>
                Hi! I'm Heather, your HR assistant. 👋
              </Text>
              <Text variant="medium" className={styles.emptySubtext}>
                Ask me anything about our HR policies and procedures!
              </Text>
            </div>
          )}

          {messages.map((msg, idx) => this._renderMessage(msg, idx))}

          {isLoading && (
            <div className={styles.messageRow + ' ' + styles.assistantRow}>
              <img src={HEATHER_AVATAR} alt="Heather" className={styles.avatar} />
              <div className={styles.messageBubble + ' ' + styles.assistantBubble}>
                <Spinner size={SpinnerSize.small} label="Heather is thinking..." labelPosition="right" />
              </div>
            </div>
          )}

          <div ref={this.chatEndRef as React.RefObject<HTMLDivElement>} />
        </div>

        {/* Input area */}
        <Stack horizontal tokens={{ childrenGap: 8 }} className={styles.inputArea}>
          <Stack.Item grow>
            <TextField
              placeholder="Ask Heather about HR policies..."
              value={inputValue}
              onChange={this._handleInputChange}
              onKeyDown={this._handleKeyDown}
              disabled={isLoading}
              borderless
              className={styles.inputField}
            />
          </Stack.Item>
          <PrimaryButton
            text="Send"
            onClick={this._sendMessage}
            disabled={isLoading || !inputValue.trim()}
            iconProps={{ iconName: 'Send' }}
            className={styles.sendButton}
          />
          <DefaultButton
            text="Clear"
            onClick={this._clearChat}
            disabled={isLoading && messages.length === 0}
            iconProps={{ iconName: 'Delete' }}
          />
        </Stack>
      </div>
    );
  }
}
