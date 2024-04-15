"use client";

import React, { useRef, useState } from 'react';

import { styled } from '@mui/material/styles';
import Paper from '@mui/material/Paper';
import TriviaQuestion from "./trivia-question/page";
import Question from "@app/models/question";

export default function Trivia() {

  const flippyRef = useRef(null);

  const Item = styled(Paper)(({ theme }) => ({
    backgroundColor: theme.palette.mode === 'dark' ? '#1A2027' : '#fff',
    ...theme.typography.body2,
    padding: theme.spacing(1),
    textAlign: 'center',
    color: theme.palette.text.secondary,
  }));

  const [currentQuestionIndex, setIndex] = useState(0);

  const _questions: Question[] = [{
    id: 1,
    text: "What is the capital of France?",
    options: [
      { id: 1, text: "Paris" },
      { id: 2, text: "London" },
      { id: 3, text: "Berlin" },
      { id: 4, text: "Rome" },
    ],
    correctOptionId: 1,
    explanation: "It is what it is"
  },
  {
    id: 2,
    text: "What is the capital of Germany?",
    options: [
      { id: 1, text: "ASD" },
      { id: 2, text: "REFa" },
      { id: 3, text: "ASD" },
      { id: 4, text: "GASD" },
    ],
    correctOptionId: 3,
    explanation: "It is what it is"
  }];

  const handleAnswerSelected = (optionId: number) => {
    // Do something with optionId
    console.log(`[Trivia] Option ${optionId} was selected. Current currentQuestionIndex=${currentQuestionIndex}`);
    if(currentQuestionIndex + 1 > _questions.length)
      setIndex(0);
    else
      setIndex(currentQuestionIndex + 1);
  };

  console.log(`[Trivia] About to render with currentQuestionIndex=${currentQuestionIndex}.`);
  return (
    <TriviaQuestion 
      question={_questions[currentQuestionIndex]} 
      onAnswerSelected={handleAnswerSelected}
    />
  );
}