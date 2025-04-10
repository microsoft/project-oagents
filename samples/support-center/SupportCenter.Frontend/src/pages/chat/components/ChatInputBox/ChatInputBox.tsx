import {
  Button,
  InputOnChangeData,
  Textarea,
  makeStyles,
  Tooltip,
} from "@fluentui/react-components";
import {
  AddSquareRegular,
  SendRegular,
  MicRegular,
  MicOffRegular,
} from "@fluentui/react-icons";
import React, { useContext, useEffect, useRef, useState } from "react";
import {
  ChatFeatureContext,
  ChatFeatureContextHandler,
} from "../../../../states/ChatContext";
import "./ChatInputBox.css";
import { AudioService } from "../../../../services/AudioService";
import { HubConnection, HubConnectionState } from "@microsoft/signalr";
import { GetStreamingConnection } from "../../../../services/ChatService";

const useStyles = makeStyles({
  button: {
    marginRight: "5px",
  },
});

export function ChatInputBox() {
  const [message, setMessage] = useState("");
  const [isVoiceActive, setIsVoiceActive] = useState(false);
  const [isVoiceReady, setIsVoiceReady] = useState(false);
  const [hubConnection, setHubConnection] = useState<HubConnection | null>(null);
  const audioService = useRef<AudioService | null>(null);
  const contextHandler = useContext(ChatFeatureContextHandler);
  const chatContext = useContext(ChatFeatureContext);
  const styles = useStyles();
  useEffect(() => {
    // Initialize the SignalR connection for voice interaction
    if (chatContext.conversation && chatContext.conversation.id) {
      const connection = GetStreamingConnection();
      
      // Ensure we have a valid connection
      if (connection && connection.state === HubConnectionState.Disconnected) {
        connection.start()
          .then(() => {
            console.log("Voice hub connection established successfully");
            setHubConnection(connection);
          })
          .catch(err => {
            console.error("Error establishing voice hub connection:", err);
            // Retry logic could be added here
          });
      } else {
        setHubConnection(connection);
      }
      
      // Create the AudioService with the connection
      if (connection) {
        audioService.current = new AudioService(
          connection,
          chatContext.conversation.metadata.userId,
          chatContext.conversation.id
        );
        
        // Set up transcription callback
        audioService.current.onTranscription((text, isPartial) => {
          if (!isPartial) {
            // When we get a final transcription, display it in the conversation
            // as a temporary message that will be replaced when the agent responds
            console.log("Final transcription received:", text);
          }
        });
        
        // Add state handler for voice ready status
        if (audioService.current) {
          const originalStartRecording = audioService.current.startRecording;
          audioService.current.startRecording = function() {
            setIsVoiceReady(true);
            originalStartRecording.call(this);
          };
        }
      }
    }
    
    return () => {
      // Cleanup function to stop voice interaction when component unmounts
      if (audioService.current && isVoiceActive) {
        audioService.current.stopVoiceInteraction(
          chatContext.conversation.metadata.userId,
          chatContext.conversation.id
        );
        setIsVoiceActive(false);
      }
    };
  }, [chatContext.conversation, isVoiceActive]);

  const toggleVoiceInteraction = async () => {
    if (!isVoiceActive) {
      if (!audioService.current) {
        console.error("AudioService not initialized");
        return;
      }
      
      try {
        setIsVoiceActive(true);
        
        // Start voice interaction with current conversation context
        await audioService.current.startVoiceInteraction(
          chatContext.conversation.metadata.userId,
          chatContext.conversation.id
        );
      } catch (error) {
        console.error("Failed to start voice interaction:", error);
        setIsVoiceActive(false);
      }
    } else {
      // Stop the voice interaction
      if (audioService.current) {
        await audioService.current.stopVoiceInteraction(
          chatContext.conversation.metadata.userId,
          chatContext.conversation.id
        );
      }
      setIsVoiceActive(false);
      setIsVoiceReady(false);
    }
  };

  function onMessageChanged(
    _ev: React.FormEvent<HTMLInputElement | HTMLTextAreaElement>,
    data?: InputOnChangeData
  ) {
    setMessage(data?.value || "");
  }

  function onEnterPress(event: React.KeyboardEvent<Element>) {
    if (event.key === "Enter" && !event.shiftKey) {
      onSendMessageClicked();
      event.preventDefault();
    }
  }

  function onSendMessageClicked() {
    if (message) {
      contextHandler.onSendMessage(message);
      setMessage("");
    }
  }

  return (
    <>
      <Textarea
        value={message}
        size="large"
        className="input-message"
        onChange={onMessageChanged}
        onKeyDown={onEnterPress}
        disabled={isVoiceActive}
        placeholder={isVoiceActive ? "Voice interaction is active..." : "Type your message here..."}
      />
      <div className="input-toolbar">
        <div>
          <Button
            icon={<AddSquareRegular />}
            size="large"
            className={styles.button}
            onClick={contextHandler.onRestartConversation}
            disabled={isVoiceActive}
          >
            Start new conversation
          </Button>
          <Button
            icon={isVoiceActive ? <MicOffRegular /> : <MicRegular />}
            size="large"
            className={styles.button}
            onClick={toggleVoiceInteraction}
            disabled={!hubConnection}
          >
            {isVoiceActive
              ? isVoiceReady
                ? "Stop Voice"
                : "Initializing..."
              : "Start Voice"}
          </Button>
        </div>
        <Button
          icon={<SendRegular />}
          appearance="primary"
          size="large"
          onClick={onSendMessageClicked}
          disabled={!message || isVoiceActive}
        ></Button>
      </div>
    </>
  );
}
