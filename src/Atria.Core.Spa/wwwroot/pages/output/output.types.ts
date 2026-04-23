import { CreateOutput, UpdateOutput } from '../../api/api.client';

export enum OutputOperation {
    Create = 'create',
    Update = 'update'
}

export interface OutputRequest {
    outputData: CreateOutput | UpdateOutput;
    outputId?: string;
    operation: OutputOperation;
}