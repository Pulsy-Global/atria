import { OutputType } from '../../api/api.client';
import { EnumOption } from '../modals/filter/filter-modal.types';

export interface ConfigBase {
}

export interface WebhookConfig extends ConfigBase {
    url: string;
    method: WebhookHttpMethod;
    headers: { [key: string]: string };
    timeoutSeconds: number;
}

export interface TelegramConfig extends ConfigBase {
    botToken: string;
    chatId: string;
    messageTemplate: string;
    enableMarkdown: boolean;
    disableWebPagePreview: boolean;
    disableNotification: boolean;
    timeoutSeconds: number;
}

export interface PostgresConfig extends ConfigBase {
    connectionString: string;
    tableName: string;
    schema: string;
    columnMappings: { [key: string]: string };
    createTableIfNotExists: boolean;
    batchSize: number;
    timeoutSeconds: number;
}

export interface EmailConfig extends ConfigBase {
    smtpServer: string;
    smtpPort: number;
    username: string;
    password: string;
    enableSsl: boolean;
    fromEmail: string;
    fromName: string;
    toEmails: string[];
    ccEmails: string[];
    bccEmails: string[];
    subject: string;
    bodyTemplate: string;
    isHtml: boolean;
    timeoutSeconds: number;
}

export interface DiscordConfig extends ConfigBase {
    webhookUrl: string;
    username: string;
    avatarUrl: string;
    message: string;
    enableTts: boolean;
    timeoutSeconds: number;
}

export enum WebhookHttpMethod {
    Post = 'Post',
    Put = 'Put',
}

export enum OutputConfigMode {
    ReadOnly = 'readonly',
    Create = 'create',
    Edit = 'edit'
}

export interface S3Config extends ConfigBase {
    bucketName: string;
    region: string;
    accessKeyId: string;
    secretAccessKey: string;
    prefix: string;
    fileFormat: string;
    compressionEnabled: boolean;
    timeoutSeconds: number;
}

export interface OutputTypeConfig extends EnumOption {
    heroIcon: string;
    color: string;
    visible: boolean;
    disabled: boolean;
}

export type OutputConfig = WebhookConfig | TelegramConfig | PostgresConfig | EmailConfig | DiscordConfig | S3Config;