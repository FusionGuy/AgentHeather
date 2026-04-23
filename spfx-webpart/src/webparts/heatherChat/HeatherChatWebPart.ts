import * as React from 'react';
import * as ReactDom from 'react-dom';
import { Version } from '@microsoft/sp-core-library';
import {
  type IPropertyPaneConfiguration,
  PropertyPaneTextField
} from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';

import { HeatherChat, IHeatherChatProps } from './components/HeatherChat';

export interface IHeatherChatWebPartProps {
  apiBaseUrl: string;
}

export default class HeatherChatWebPart extends BaseClientSideWebPart<IHeatherChatWebPartProps> {

  public render(): void {
    const element: React.ReactElement<IHeatherChatProps> = React.createElement(
      HeatherChat,
      {
        apiBaseUrl: this.properties.apiBaseUrl || 'https://heather-demo-chat.azurewebsites.net',
        context: this.context
      }
    );

    ReactDom.render(element, this.domElement);
  }

  protected onDispose(): void {
    ReactDom.unmountComponentAtNode(this.domElement);
  }

  protected get dataVersion(): Version {
    return Version.parse('1.0');
  }

  protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration {
    return {
      pages: [
        {
          header: {
            description: 'Configure the Heather Chat web part'
          },
          groups: [
            {
              groupName: 'Connection Settings',
              groupFields: [
                PropertyPaneTextField('apiBaseUrl', {
                  label: 'API Base URL',
                  description: 'The base URL of the Heather Demo App (e.g., https://heather-demo-chat.azurewebsites.net)',
                  value: this.properties.apiBaseUrl
                })
              ]
            }
          ]
        }
      ]
    };
  }
}
