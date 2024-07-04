import { Button } from "@fluentui/react-components";
import {
  Drawer,
  DrawerBody,
  DrawerHeader,
  DrawerHeaderTitle,
} from "@fluentui/react-components/unstable";
import { Dismiss24Regular, Open24Regular } from "@fluentui/react-icons";
import { HubConnection } from "@microsoft/signalr";
import { useCallback, useEffect, useState } from "react";
import { v4 as uuid } from "uuid";
import { WelcomeHints } from "../../components/WelcomeHints/WelcomeHints";
import { Conversation } from "../../models/Conversation";
import { Message, SenderType } from "../../models/Message";
import {
  GetStreamingConnection,
  SendFeedbackAsync,
  getConfigurationAsync,
} from "../../services/ChatService";
import { AppFeatureContext, appInitialContext } from "../../states/AppContext";
import {
  ChatFeatureContext,
  ChatFeatureContextHandler,
  initialContext as chatInitialContext,
  initialContextHandler,
} from "../../states/ChatContext";
import { pageStyles } from "../PageStyles";
import { ChatHistoryList } from "./components/ChatHistoryList/ChatHistoryList";
import { ChatInputBox } from "./components/ChatInputBox/ChatInputBox";

export function ChatPage() {
  const styles = pageStyles();
  const [appContext, setAppContext] = useState(appInitialContext);
  const [context, setContext] = useState(chatInitialContext);
  const [contextHandler, setContextHandler] = useState(initialContextHandler);
  const [streamingConnection, setStreamingConnection] =
    useState<HubConnection | null>(null);

  useEffect(() => {
    setContextHandler((h) => ({
      ...h,
      onSendMessage: onSendMessage,
      onSendFeedback: onSendFeedback,
      onRestartConversation: onRestartConversation,
    }));
  }, [context]);

  useEffect(() => {
    console.log("InitApplication");

    const getConfiguration = async () => {
      const configuration = await getConfigurationAsync();
      setAppContext((c) => ({ ...c, configuration: configuration }));
      initConversation();
    };

    getConfiguration();
  }, []);

  const initConversation = async () => {
    console.log("initConversation");
    const conversationId: string = uuid();
    const metadata = { userId: "1234" };
    const connection = GetStreamingConnection();
    // Start the SignalR connection
    connection
      .start()
      .then(() =>
        connection.invoke("ConnectToAgent", metadata.userId, conversationId)
      )
      .catch((err) => console.error("SignalR connection error:", err));
    console.log("SignalR connection established");
    setStreamingConnection(connection);

    // Register the ReceiveMessage event
    connection.on("ReceiveMessage", (message: Message) => {
      console.log("Received message:", message);
      // if (message.sender === SenderType.Notification) {
      //   const progressMessage = message.text;
      //   setContext((c) => ({
      //     ...c,
      //     isTakingTooLong: true,
      //     waitingMessage: progressMessage,
      //   }));
      // } else {
      setContext((c) => ({
        ...c,
        isLoading: false,
        isTakingTooLong: false,
        conversation: {
          ...c.conversation,
          messages: [
            ...c.conversation.messages,
            { ...message, sender: message.sender},
          ],
        },
      }));
      // }
    });

    setContext((c) => ({
      ...c,
      conversation: { id: conversationId, metadata, messages: [] },
    }));
  };

  const onSendMessage = useCallback(
    (messageText: string) => {
      console.log("SendMessage:" + context.conversation.id + " " + messageText);

      const sendMessage = async () => {
        const userMessage: Message = {
          id: uuid().toString(),
          conversationId: context.conversation.id,
          userId: context.conversation.metadata.userId,
          sender: SenderType.User,
          text: messageText,
          timestamp: new Date(),
          citations: undefined,
          feedback: undefined,
          isError: false,
        };

        setContext((c) => ({
          ...c,
          isLoading: true,
          conversation: {
            ...c.conversation,
            messages: [...c.conversation.messages, userMessage],
          },
        }));

        streamingConnection?.send("ProcessMessage", userMessage);

        const waitTime = 20000;
        const waitingMessages =
          "Please wait while the agents are working for you... | Please wait while the agents are looking for a solution...";

        setTimeout(() => {
          const messages = waitingMessages.split("|");
          const messageToDisplay = `${
            messages[Math.floor(Math.random() * messages.length)]
          }`;
          setContext((c) => ({
            ...c,
            isTakingTooLong: true,
            waitingMessage: messageToDisplay,
          }));
        }, waitTime);
      };
      sendMessage();
    },
    [context, streamingConnection]
  );

  function onSendFeedback(message: Message, feedback: -1 | 1) {
    console.log("onSendFeedback");

    const sendFeedbackAsync = async () =>
      await SendFeedbackAsync(
        context.conversation.id,
        message.id,
        feedback.toString(),
        message.text
      );

    sendFeedbackAsync();

    const conversation = context.conversation;
    const updatedConversation: Conversation = {
      ...conversation,
      messages: conversation.messages.map((m) =>
        m.id === message.id ? { ...m, feedback: feedback } : m
      ),
    };

    setContext((c) => ({ ...c, conversation: updatedConversation }));
  }

  const onRestartConversation = useCallback(async () => {
    console.log("onRestartConversation");

    if (streamingConnection) {
      try {
        const newConversationId = uuid();
        await streamingConnection.invoke(
          "RestartConversation",
          context.conversation.metadata.userId,
          newConversationId
        );
        setContext((c) => ({
          ...c,
          conversation: {
            id: newConversationId,
            metadata: c.conversation.metadata,
            messages: [],
          },
        }));
      } catch (error) {
        console.error("Error restarting conversation: ", error);
      }
    }
  }, [streamingConnection, context]);

  return (
    <>
      <AppFeatureContext.Provider value={appContext}>
        {appContext.configuration?.chatConfiguration ? (
          <ChatFeatureContext.Provider value={context}>
            <ChatFeatureContextHandler.Provider value={contextHandler}>
              <div className={styles.sectionContainer}>
                {!context.conversation.messages ||
                context.conversation.messages.length === 0 ? (
                  <WelcomeHints />
                ) : (
                  <ChatHistoryList />
                )}
              </div>
              <div className={styles.sectionContainer}>
                <ChatInputBox />
              </div>

              <Drawer
                type="overlay"
                size="large"
                position="end"
                separator
                open={!!context.citationPreview}
              >
                <DrawerHeader>
                  <DrawerHeaderTitle
                    action={
                      <>
                        <Button
                          appearance="subtle"
                          aria-label="Open file"
                          icon={<Open24Regular />}
                          onClick={() =>
                            window.open(context.citationPreview?.url, "_blank")
                          }
                        />
                        <Button
                          appearance="subtle"
                          aria-label="Close"
                          icon={<Dismiss24Regular />}
                          onClick={() =>
                            setContext((c) => ({
                              ...c,
                              citationPreview: undefined,
                            }))
                          }
                        />
                      </>
                    }
                  >
                    {context.citationPreview?.title}
                  </DrawerHeaderTitle>
                </DrawerHeader>

                <DrawerBody>
                  {appContext.configuration?.chatConfiguration
                    .previewContent === "File" ? (
                    <embed
                      src={context.citationPreview?.url}
                      className="pdf-content"
                      width="100%"
                      height="95%"
                    />
                  ) : (
                    <p>{context.citationPreview?.content}</p>
                  )}
                </DrawerBody>
              </Drawer>
            </ChatFeatureContextHandler.Provider>
          </ChatFeatureContext.Provider>
        ) : (
          <></>
        )}
      </AppFeatureContext.Provider>
    </>
  );
}
