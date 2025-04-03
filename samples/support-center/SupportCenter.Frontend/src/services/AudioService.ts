import { HubConnection } from "@microsoft/signalr";

export class AudioService {
    private hubConnection: HubConnection;
    private mediaRecorder: MediaRecorder | null = null;
    private readonly userId: string;
    private readonly conversationId: string;
    private isReady: boolean = false;
    private audioContext: AudioContext | null = null;
    private onTranscriptionCallback: ((text: string, isPartial: boolean) => void) | null = null;
    private onAudioResponseCallback: ((audioData: ArrayBuffer) => void) | null = null;
    private transcriptionBuffer: string = '';

    constructor(hubConnection: HubConnection, userId: string, conversationId: string) {
        this.hubConnection = hubConnection;
        this.userId = userId;
        this.conversationId = conversationId;

        // Initialize audio context lazily
        this.createAudioContext();
        
        // Set up event handlers for the hub connection
        this.hubConnection.on("VoiceInteractionReady", () => {
            this.isReady = true;
            this.startRecording();  // Start recording once ready
        });
        
        // Handle partial transcriptions during streaming
        this.hubConnection.on("ReceivePartialTranscription", (text: string) => {
            this.transcriptionBuffer += text;
            if (this.onTranscriptionCallback) {
                this.onTranscriptionCallback(text, true);
            }
        });
        
        // Handle final transcription
        this.hubConnection.on("ReceiveTranscription", (result: { text: string, isComplete: boolean }) => {
            if (this.onTranscriptionCallback) {
                this.onTranscriptionCallback(result.text, !result.isComplete);
            }
            
            // Reset transcription buffer when we get a complete transcription
            if (result.isComplete) {
                this.transcriptionBuffer = '';
            }
        });
        
        // Handle audio responses from GPT-4o
        this.hubConnection.on("ReceiveAudioResponse", async (audioData: Uint8Array) => {
            if (this.onAudioResponseCallback) {
                this.onAudioResponseCallback(audioData.buffer);
            } else {
                // Default handler: play the audio
                await this.playAudioResponse(audioData.buffer);
            }
        });
    }
    
    private createAudioContext() {
        if (!this.audioContext) {
            // Use AudioContext with fallback for older browsers
            const AudioContext = window.AudioContext || (window as any).webkitAudioContext;
            if (AudioContext) {
                this.audioContext = new AudioContext();
            }
        }
    }

    async startVoiceInteraction(userId: string, conversationId: string): Promise<void> {
        try {
            // Request microphone permission
            const stream = await navigator.mediaDevices.getUserMedia({ 
                audio: {
                    channelCount: 1,
                    sampleRate: 16000,
                    echoCancellation: true,
                    noiseSuppression: true
                } 
            }).catch((err) => {
                if (err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError') {
                    throw new Error('Microphone permission denied. Please allow microphone access to use voice features.');
                } else if (err.name === 'NotFoundError' || err.name === 'DevicesNotFoundError') {
                    throw new Error('No microphone found. Please connect a microphone and try again.');
                } else {
                    throw err;
                }
            });

            // Use 16kHz sample rate for better speech recognition
            try {
                this.mediaRecorder = new MediaRecorder(stream, {
                    mimeType: 'audio/webm;codecs=opus',
                    audioBitsPerSecond: 16000
                });
            } catch (codecError) {
                // Fallback if the specified codec isn't supported
                console.warn('Specified codec not supported, using default codec', codecError);
                this.mediaRecorder = new MediaRecorder(stream);
            }

            await this.setupMediaRecorder();

            // Reset transcription buffer
            this.transcriptionBuffer = '';

            try {
                // Signal backend to start a RealTime audio session
                await this.hubConnection.invoke('StartVoiceInteraction', userId, conversationId);
            } catch (connectionError) {
                // Clean up resources if connection to backend fails
                stream.getTracks().forEach(track => track.stop());
                this.mediaRecorder = null;
                throw new Error('Could not connect to voice service. Please try again later.');
            }

            // Recording will start once the backend signals that it is ready
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
                    
                    // Send audio data to backend for realtime processing
                    // The backend will handle the transcription using GPT-4o Realtime API
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

    async stopVoiceInteraction(userId: string, conversationId: string): Promise<void> {
        if (this.mediaRecorder) {
            try {
                // Stop recording first
                this.mediaRecorder.stop();
                this.mediaRecorder.stream.getTracks().forEach(track => track.stop());
                
                // End the voice interaction session on the backend
                try {
                    await this.hubConnection.invoke('EndVoiceInteraction', userId, conversationId);
                } catch (connectionError) {
                    console.warn('Could not properly end voice session on server:', connectionError);
                    // Even if server communication fails, we still want to clean up local resources
                }
            } catch (error) {
                console.error('Error stopping voice interaction:', error);
            } finally {
                // Always reset state even if there are errors
                this.isReady = false;
                this.mediaRecorder = null;
            }
        }
    }
    
    // Method to play audio response
    async playAudioResponse(audioBuffer: ArrayBuffer): Promise<void> {
        if (!this.audioContext) {
            this.createAudioContext();
        }
        
        if (!this.audioContext) {
            console.error("AudioContext not available");
            return;
        }
        
        try {
            // Decode audio data
            const audioData = await this.audioContext.decodeAudioData(audioBuffer);
            
            // Create source node
            const source = this.audioContext.createBufferSource();
            source.buffer = audioData;
            
            // Connect to audio output
            source.connect(this.audioContext.destination);
            
            // Play the audio
            source.start(0);
        } catch (error) {
            console.error("Error playing audio response:", error);
        }
    }
    
    // Register callback for transcription updates
    onTranscription(callback: (text: string, isPartial: boolean) => void): void {
        this.onTranscriptionCallback = callback;
    }
    
    // Register callback for audio responses
    onAudioResponse(callback: (audioData: ArrayBuffer) => void): void {
        this.onAudioResponseCallback = callback;
    }
    
    // Get the current transcription buffer
    getCurrentTranscription(): string {
        return this.transcriptionBuffer;
    }
}
