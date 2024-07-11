import {
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
  Avatar,
  Button,
  Caption1,
  Card,
  Divider,
  InteractionTag,
  InteractionTagPrimary,
  Popover,
  PopoverSurface,
  PopoverTrigger,
  PositioningImperativeRef,
  Skeleton,
  SkeletonItem,
  TagGroup,
  makeStyles,
  shorthands,
} from "@fluentui/react-components";
import {
  BotRegular,
  Library16Regular,
  ThumbDislike24Filled,
  ThumbDislikeRegular,
  ThumbLike24Filled,
  ThumbLikeRegular,
} from "@fluentui/react-icons";
import { useContext, useEffect, useMemo, useRef } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import supersub from "remark-supersub";
import { Message, SenderType } from "../../../../models/Message";
import { ChatFeatureContextHandler } from "../../../../states/ChatContext";
import "./ChatMessage.css";
import { parseMessage } from "./MessageParser";

const useStyles = makeStyles({
  copilotMessage: {
    minWidth: "300px",
    maxWidth: "600px",
  },
  errorMessage: {
    minWidth: "300px",
    maxWidth: "600px",
    color: "red",
  },
  userMessage: {
    minWidth: "150px",
    maxWidth: "600px",
    backgroundColor: "#EDF5FD",
  },
  /* Agent message styles */
  // Dispatcher
  dispatcherMessage: {
    minWidth: "300px",
    maxWidth: "600px",
    backgroundColor: "#FAD7A0",
  },
  // CustomerInfo
  customerInfoMessage: {
    minWidth: "300px",
    maxWidth: "600px",
    backgroundColor: "#D5F5E3",
  },
  // Notification
  notificationMessage: {
    minWidth: "300px",
    maxWidth: "600px",
    backgroundColor: "#FFF3CD",
  },
  // Discount
  discountMessage: {
    minWidth: "300px",
    maxWidth: "600px",
    backgroundColor: "#63F09B",
  },
  // Invoice
  invoiceMessage: {
    minWidth: "300px",
    maxWidth: "600px",
    backgroundColor: "#E12FCF",
  },
  // QnA
  qnaMessage: {
    minWidth: "300px",
    maxWidth: "600px",
    backgroundColor: "#E6AE2A",
  },
  avatar: {
    ...shorthands.margin("0px", "5px"),
  },
  skeletonItem: {
    ...shorthands.margin("5px", "0px"),
  },
  feedbackContainer: {
    display: "flex",
    ...shorthands.padding("4px"),
  },
});

interface ChatMessageProps {
  message?: Message;
  isLoading?: boolean;
  isTakingTooLong?: boolean;
  waitingMessage?: string;
}

export function ChatMessage({
  message,
  isLoading,
  isTakingTooLong,
  waitingMessage,
}: ChatMessageProps) {
  const username = "User"; // TODO: Get username from context

  const feedbackRef = useRef<HTMLDivElement>(null);
  const positioningRef = useRef<PositioningImperativeRef>(null);
  const styles = useStyles();
  const contextHandler = useContext(ChatFeatureContextHandler);

  const parsedMessage: Message | undefined = useMemo(
    () => parseMessage(message),
    [message]
  );

  useEffect(() => {
    if (feedbackRef.current) {
      positioningRef.current?.setTarget(feedbackRef.current);
    }
  }, [feedbackRef, positioningRef]);

  const getMessageStyle = (senderType: string) => {
    switch (senderType) {
      case "CustomerInfo":
      case "CustomerInfoNotification":
        return styles.customerInfoMessage;
      case "Dispatcher":
      case "DispatcherNotification":
        return styles.dispatcherMessage;
      case "Notification":
        return styles.notificationMessage;
      case "Discount":
        return styles.discountMessage;
      case "Invoice":
        return styles.invoiceMessage;
      case "QnA":
        return styles.qnaMessage;
      // Add more cases as needed
      default:
        return styles.copilotMessage;
    }
  };

  const getAgentInitials = (senderType: SenderType) => {
    switch (senderType) {
      case SenderType.Agent:
        return "A";
      case SenderType.CustomerInfo:
      case SenderType.CustomerInfoNotification:
        return "C I";
      case SenderType.Dispatcher:
      case SenderType.DispatcherNotification:
        return "D";
      case SenderType.Notification:
        return "N";
      case SenderType.Discount:
        return "D I";
      case SenderType.Invoice:
        return "I";
      case SenderType.QnA:
        return "Q A";
      // Add more cases as needed
      default:
        return "A"; // Default to Agent
    }
  };

  switch (true) {
    case isLoading:
      return (
        <div className="message-container">
          <Avatar
            color="colorful"
            icon={<BotRegular />}
            className={styles.avatar}
          />
          <Card className={styles.copilotMessage}>
            <Skeleton>
              {isTakingTooLong ? (
                <ReactMarkdown remarkPlugins={[remarkGfm, supersub]}>
                  {waitingMessage}
                </ReactMarkdown>
              ) : (
                <></>
              )}
              <SkeletonItem className={styles.skeletonItem} size={16} />
              <SkeletonItem className={styles.skeletonItem} size={8} />
              <SkeletonItem className={styles.skeletonItem} size={16} />
              <SkeletonItem className={styles.skeletonItem} size={12} />
              <Divider>
                <Caption1 className="message-disclaimer">
                  Generating answer
                </Caption1>
              </Divider>
            </Skeleton>
          </Card>
        </div>
      );
    case message &&
      message.sender !== SenderType.User &&
      (message.isError == false || message.isError == undefined):
      return (
        <div className="message-container">
          <Avatar
            color="colorful"
            name={getAgentInitials(message.sender)}
            icon={<BotRegular />}
            className={styles.avatar}
          />
          <Popover
            positioning={{ positioningRef }}
            openOnHover={true}
            unstable_disableAutoFocus={true}
          >
            <PopoverTrigger>
              <Card className={getMessageStyle(message.sender)}>
                <div className="feedback-container-target">
                  <div ref={feedbackRef}></div>
                </div>
                <ReactMarkdown remarkPlugins={[remarkGfm, supersub]}>
                  {parsedMessage?.text}
                </ReactMarkdown>
                <Divider>
                  <Caption1 className="message-disclaimer">
                    AI-generated content
                  </Caption1>
                </Divider>
                {parsedMessage?.citations &&
                parsedMessage.citations?.length > 0 ? (
                  <Accordion collapsible>
                    <AccordionItem value="references">
                      <AccordionHeader icon={<Library16Regular />} size="small">
                        References
                      </AccordionHeader>
                      <AccordionPanel>
                        <TagGroup className="citation-container">
                          {parsedMessage.citations?.map((citation, index) => (
                            <InteractionTag
                              key={citation.id}
                              size="extra-small"
                              appearance="brand"
                              className="citation-item"
                              onClick={() =>
                                contextHandler.onCitationPreview(citation)
                              }
                            >
                              <InteractionTagPrimary>
                                {index + 1 + ". " + citation.title}
                              </InteractionTagPrimary>
                            </InteractionTag>
                          ))}
                        </TagGroup>
                      </AccordionPanel>
                    </AccordionItem>
                  </Accordion>
                ) : (
                  <></>
                )}
              </Card>
            </PopoverTrigger>
            <PopoverSurface className={styles.feedbackContainer}>
              {message?.feedback === 1 ? (
                <ThumbLike24Filled color="#c4dfb8" />
              ) : (
                <Button
                  size="small"
                  appearance="subtle"
                  icon={<ThumbLikeRegular />}
                  onClick={() => contextHandler.onSendFeedback(message!, 1)}
                />
              )}
              {message?.feedback === -1 ? (
                <ThumbDislike24Filled color="#ffc7cd" />
              ) : (
                <Button
                  size="small"
                  appearance="subtle"
                  icon={<ThumbDislikeRegular />}
                  onClick={() => contextHandler.onSendFeedback(message!, -1)}
                />
              )}
            </PopoverSurface>
          </Popover>
        </div>
      );
    case message &&
      message.sender !== SenderType.User &&
      message.isError == true:
      return (
        <div className="message-container">
          <Avatar
            color="colorful"
            name={getAgentInitials(message.sender)}
            icon={<BotRegular />}
            className={styles.avatar}
          />
          <Card className={styles.errorMessage}>
            <ReactMarkdown remarkPlugins={[remarkGfm, supersub]}>
              {parsedMessage?.text}
            </ReactMarkdown>
          </Card>
        </div>
      );
    case message && message.sender === SenderType.User:
      return (
        <div className="message-container">
          <Card className={styles.userMessage}>{message!.text}</Card>
          <Avatar color="colorful" name={username} className={styles.avatar} />
        </div>
      );
    default:
      return <></>;
  }
}
