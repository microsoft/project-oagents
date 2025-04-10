import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

export class SignalRService {
    private static instance: SignalRService;
    private hubConnection: HubConnection | null = null;
    private supportCenterBaseUrl: string = import.meta.env.VITE_OAGENT_BASE_URL;

    private constructor() { }

    public static getInstance(): SignalRService {
        if (!SignalRService.instance) {
            SignalRService.instance = new SignalRService();
        }
        return SignalRService.instance;
    }
    public initializeConnection(): HubConnection {
        if (!this.hubConnection) {
            try {
                const url = new URL('supportcenterhub', this.supportCenterBaseUrl).href;
                console.log('Initializing SignalR connection to:', url);
                this.hubConnection = new HubConnectionBuilder()
                    .withUrl(url)
                    .withAutomaticReconnect()
                    .build();
            } catch (err) {
                console.error('Error initializing SignalR connection:', err);
                throw err;
            }
        }
        return this.hubConnection;
    }

    public async startConnection(): Promise<void> {
        try {
            await this.hubConnection?.start();
            console.log('SignalR connection started');
        } catch (err) {
            console.error('Error starting SignalR connection:', err);
            throw err;
        }
    }

    public async stopConnection(): Promise<void> {
        try {
            await this.hubConnection?.stop();
            console.log('SignalR connection stopped');
        } catch (err) {
            console.error('Error stopping SignalR connection:', err);
            throw err;
        }
    }

    public onMessage(callback: (message: any) => void): void {
        this.hubConnection?.on('ReceiveMessage', callback);
    }

    public getConnection(): HubConnection | null {
        return this.hubConnection;
    }
}