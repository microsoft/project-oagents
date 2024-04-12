"use client";
import React, { useState } from 'react';

import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';

import { styled } from '@mui/material/styles';
import Paper from '@mui/material/Paper';


export default function TriviaOption({ OptionNumber, OptionText, onClick }) {

  const Item = styled(Paper)(({ theme }) => ({
    backgroundColor: theme.palette.mode === 'dark' ? '#1A2027' : '#fff',
    ...theme.typography.body2,
    padding: theme.spacing(1),
    textAlign: 'center',
    color: theme.palette.text.secondary,
  }));

  const [isHovered, setIsHovered] = useState(false);

  return (
    <Paper 
      onMouseEnter={() => setIsHovered(true)} 
      onMouseLeave={() => setIsHovered(false)}
      style={{ backgroundColor: isHovered ? 'lightgray' : 'white' }}
      onClick={onClick}
    >
      <CardContent>
        <Typography variant="body2" color="text.secondary">
          {OptionNumber}
        </Typography>
        <Typography gutterBottom variant="h5" component="div">
          {OptionText}
        </Typography>
      </CardContent>
  </Paper>
  );
}
