import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiClient } from './api.client';
import { AppConfigService } from 'shared/core/config/app.config.service';
import './api.extensions';

@Injectable({
    providedIn: 'root'
})
export class ApiService {
    public apiClient: ApiClient;
    public apiServer: string;

    constructor(
        private httpClient: HttpClient,
        private appConfigService: AppConfigService,
    ) {
        this.apiServer = this.appConfigService.apiServer;

        this.apiClient = new ApiClient(
            this.httpClient, 
            this.apiServer);
    }
}