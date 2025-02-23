import { HubConnection } from "@microsoft/signalr";

export class AudioService {
    private hubConnection: HubConnection;
    private mediaRecorder: MediaRecorder | null = null;
    private audioContext: AudioContext;
    private audioQueue: AudioBuffer[] = [];
    private isProcessing: boolean = false;
    private readonly userId: string;
    private readonly conversationId: string;
    private isReady: boolean = false;

    constructor(hubConnection: HubConnection, userId: string, conversationId: string) {
        this.hubConnection = hubConnection;
        this.audioContext = new AudioContext();
        this.userId = userId;
        this.conversationId = conversationId;

        this.hubConnection.on("VoiceInteractionReady", () => {
            this.isReady = true;
            this.startRecording();  // Start recording once ready
        });
    }

    async startVoiceInteraction(userId: string, conversationId: string): Promise<void> {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

            // Use existing AudioContext – no redundant creation
            this.mediaRecorder = new MediaRecorder(stream, {
                mimeType: 'audio/webm;codecs=opus',
                audioBitsPerSecond: 16000
            });

            this.setupMediaRecorder();

            // Signal backend to start a RealTime audio session.
            await this.hubConnection.invoke('StartVoiceInteraction', userId, conversationId);

            // Recording will start once the backend signals that it is ready (VoiceInteractionReady).
        } catch (error) {
            console.error('Error starting voice interaction:', error);
            throw error;
        }
    }

    async setupMediaRecorder(): Promise<void> {
        if (!this.mediaRecorder) return;

        this.mediaRecorder.ondataavailable = async (event) => {
            if (event.data.size > 0 && this.isReady) {
                try {
                    const audioData = await event.data.arrayBuffer();
                    await this.hubConnection.invoke(
                        'ProcessVoiceInput',
                        this.userId,
                        this.conversationId,
                        new Uint8Array(audioData)
                    );
                } catch (error) {
                    console.error('Error processing voice input:', error);
                }
            }
        };
    }

    startRecording() {
        if (this.mediaRecorder && this.isReady) {
            this.mediaRecorder.start(100); // Send audio chunks every 100ms
            console.log('Started recording');
        }
    }

    stopVoiceInteraction(userId: string, conversationId: string): void {
        if (this.mediaRecorder) {
            this.mediaRecorder.stop();
            this.mediaRecorder.stream.getTracks().forEach(track => track.stop());
            this.hubConnection.invoke('EndVoiceInteraction', userId, conversationId);
        }
    }
}
