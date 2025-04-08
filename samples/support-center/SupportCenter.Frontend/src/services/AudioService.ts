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
    private audioQueue: Uint8Array[] = [];
    private isProcessingAudio: boolean = false;
    
    constructor(hubConnection: HubConnection, userId: string, conversationId: string) {
        this.hubConnection = hubConnection;
        this.userId = userId;
        this.conversationId = conversationId;

        // Initialize audio context lazily
        this.createAudioContext();
        
        // Set up event handlers for the hub connection
        this.hubConnection.on("VoiceInteractionReady", () => {
            console.log("Voice interaction ready, starting recording");
            this.isReady = true;
            this.startRecording();  // Start recording once ready
        });
        
        // Handle partial transcriptions during streaming
        this.hubConnection.on("ReceivePartialTranscription", (text: string) => {
            console.log("Received partial transcription:", text);
            this.transcriptionBuffer += text;
            if (this.onTranscriptionCallback) {
                this.onTranscriptionCallback(text, true);
            }
        });
        
        // Handle final transcription
        this.hubConnection.on("ReceiveTranscription", (result: { text: string, isComplete: boolean }) => {
            console.log("Received complete transcription:", result);
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
            console.log("Received audio response from GPT-4o");
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
                this.audioContext = new AudioContext({
                    sampleRate: 16000 // Use 16kHz for better speech recognition
                });
            }
        }
    }

    async startVoiceInteraction(userId: string, conversationId: string): Promise<void> {
        try {
            // Reset state
            this.audioQueue = [];
            this.transcriptionBuffer = '';
            this.isProcessingAudio = false;
            
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
                console.log("Invoking StartVoiceInteraction on backend");
                // Signal backend to start a RealTime audio session
                await this.hubConnection.invoke('StartVoiceInteraction', userId, conversationId);
            } catch (connectionError) {
                console.error("Error starting voice interaction:", connectionError);
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
            if (event.data.size > 0) {
                try {
                    const audioData = await event.data.arrayBuffer();
                    const audioBytes = new Uint8Array(audioData);
                    
                    // Add to queue for processing
                    this.audioQueue.push(audioBytes);
                    
                    // Start processing if not already doing so
                    if (!this.isProcessingAudio && this.isReady) {
                        this.processAudioQueue();
                    }
                } catch (error) {
                    console.error('Error processing audio data:', error);
                }
            }
        };
        
        // Handle recording stopped
        this.mediaRecorder.onstop = () => {
            console.log('MediaRecorder stopped');
        };
        
        // Handle recording errors
        this.mediaRecorder.onerror = (event) => {
            console.error('MediaRecorder error:', event);
        };
    }
    
    private async processAudioQueue() {
        this.isProcessingAudio = true;
        
        while (this.audioQueue.length > 0 && this.isReady) {
            const audioBytes = this.audioQueue.shift();
            if (audioBytes) {
                try {
                    console.log(`Processing audio chunk of size: ${audioBytes.length} bytes`);
                    // Send audio data to backend for realtime processing
                    await this.hubConnection.invoke(
                        'ProcessVoiceInput',
                        this.userId,
                        this.conversationId,
                        audioBytes
                    );
                } catch (error) {
                    console.error('Error sending audio to server:', error);
                    break;
                }
            }
            
            // Small delay to prevent overwhelming the server
            await new Promise(resolve => setTimeout(resolve, 50));
        }
        
        this.isProcessingAudio = false;
    }

    startRecording() {
        if (this.mediaRecorder && this.isReady) {
            this.mediaRecorder.start(100); // Send audio chunks every 100ms
            console.log('Started recording');
        } else {
            console.warn('Cannot start recording - recorder not ready or not initialized');
        }
    }

    async stopVoiceInteraction(userId: string, conversationId: string): Promise<void> {
        if (this.mediaRecorder) {
            try {
                console.log('Stopping voice interaction');
                
                // Stop recording first
                this.mediaRecorder.stop();
                this.mediaRecorder.stream.getTracks().forEach(track => track.stop());
                
                // Process any remaining audio in the queue
                if (this.audioQueue.length > 0) {
                    await this.processAudioQueue();
                }
                
                // End the voice interaction session on the backend
                try {
                    console.log('Invoking EndVoiceInteraction on backend');
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
                this.audioQueue = [];
                this.isProcessingAudio = false;
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
