import {
  Button,
  InputOnChangeData,
  Textarea,
  makeStyles,
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
import { HubConnection } from "@microsoft/signalr";

const useStyles = makeStyles({
  button: {
    marginRight: "5px",
  },
});

export function ChatInputBox() {
  const [message, setMessage] = useState("");
  const [isVoiceActive, setIsVoiceActive] = useState(false);
  const [isVoiceReady, setIsVoiceReady] = useState(false);
  const [hubConnection] = useState<HubConnection | null>(null);
  const audioService = useRef<AudioService | null>(null);
  const contextHandler = useContext(ChatFeatureContextHandler);
  const chatContext = useContext(ChatFeatureContext); // Add this
  const styles = useStyles();

  useEffect(() => {
    if (hubConnection && chatContext.conversation) {
      audioService.current = new AudioService(
        hubConnection,
        chatContext.conversation.metadata.userId,
        chatContext.conversation.id
      );
    }
  }, [hubConnection, chatContext.conversation]);

  useEffect(() => {
    if (audioService.current) {
      // Add state handler for voice ready status
      const originalStartRecording = audioService.current.startRecording;
      audioService.current.startRecording = () => {
        setIsVoiceReady(true);
        originalStartRecording.call(audioService.current);
      };
    }
  }, []);

  const toggleVoiceInteraction = async () => {
    if (!isVoiceActive) {
      try {
        setIsVoiceActive(true);
        await audioService.current?.startVoiceInteraction(
          chatContext.conversation.metadata.userId,
          chatContext.conversation.id
        );
      } catch (error) {
        console.error("Failed to start voice interaction:", error);
        setIsVoiceActive(false);
      }
    } else {
      audioService.current?.stopVoiceInteraction(
        chatContext.conversation.metadata.userId,
        chatContext.conversation.id
      );
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
      />
      <div className="input-toolbar">
        <div>
          <Button
            icon={<AddSquareRegular />}
            size="large"
            className={styles.button}
            onClick={contextHandler.onRestartConversation}
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
