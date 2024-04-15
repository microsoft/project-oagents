type Question = {
    id: number;
    text: string;
    options: { id: number; text: string }[];
    correctOptionId: number;
    explanation: string;
};
