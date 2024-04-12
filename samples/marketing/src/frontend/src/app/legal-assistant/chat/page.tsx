"use client";

import * as React from 'react';
import Box from '@mui/material/Box';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Public from '@mui/icons-material/Public';
import KeyboardArrowDown from '@mui/icons-material/KeyboardArrowDown';
import HandshakeTwoToneIcon from '@mui/icons-material/HandshakeTwoTone';
import WorkspacePremiumTwoToneIcon from '@mui/icons-material/WorkspacePremiumTwoTone';
import { styled } from '@mui/material/styles';
import Paper from '@mui/material/Paper';
import LightbulbIcon from '@mui/icons-material/Lightbulb';

const data = [
  { icon: <HandshakeTwoToneIcon />, label: 'Bank vs Mrs Peters - Settled - chf 1.5M - 1 year' },
  { icon: <WorkspacePremiumTwoToneIcon />, label: 'Bank vs Mr Pertussi - Won - chf0 - 4 years' },
  { icon: <Public />, label: 'Bank vs Governnent - Public Case - chf 3.7M - 10 years' },
];

const FireNav = styled(List)<{ component?: React.ElementType }>({
  '& .MuiListItemButton-root': {
    paddingLeft: 24,
    paddingRight: 24,
  },
  '& .MuiListItemIcon-root': {
    minWidth: 0,
    marginRight: 16,
  },
  '& .MuiSvgIcon-root': {
    fontSize: 20,
  },
});
export default function Chat() {

  const [open, setOpen] = React.useState(true);
  const [messages, setMessages] = React.useState([]);

  const handleSend = (message) => {
    setMessages([...messages, { sender: 'user', text: message }]);
    // Here you 60would typically call your AI API and then add its response to the messages
    setMessages([...messages, { sender: 'user', text: message }, { sender: 'copilot', text: 'Your AI response here' }]);
  };

  return (
    <Box sx={{ display: 'flex' }}>
      <FireNav component="nav" disablePadding>
        <Box
          sx={{
            //bgcolor: open ? 'rgba(71, 98, 130, 0.2)' : null,
            //pb: open ? 2 : 0,
          }}
        >
          <ListItemButton
            alignItems="flex-start"
            onClick={() => setOpen(!open)}
            sx={{
              px: 3,
              pt: 2.5,
              pb: open ? 0 : 2.5,
              '&:hover, &:focus': { '& #arrowdownicon': { opacity: open ? 1 : 0 } },
            }}
          >
            <ListItemIcon sx={{ my: 0, opacity: 1, class: "menuicon" }}>
              <LightbulbIcon />
            </ListItemIcon>
            <ListItemText
              primary="Tips"
              primaryTypographyProps={{
                fontSize: 15,
                fontWeight: 'medium',
                lineHeight: '20px',
                mb: '2px',
              }}
              secondary="Tips on how to reply"
              secondaryTypographyProps={{
                noWrap: true,
                fontSize: 12,
                lineHeight: '16px',
                color: open ? 'rgba(0,0,0,0)' : 'rgba(255,255,255,0.5)',
              }}
              sx={{ my: 0 }}
            />
            <KeyboardArrowDown
              id="arrowdownicon"
              sx={{
                mr: -1,
                opacity: 0,
                transform: open ? 'rotate(-180deg)' : 'rotate(0)',
                transition: '0.2s',
              }}
            />
          </ListItemButton>
          {open && (

            <Paper>

              <div>
                <h2>Chat Copilot</h2>
                <div style={{ margin: '0 auto' }}>
                  {messages.map((message, index) => (
                    <div key={index} style={{
                      margin: '10px',
                      padding: '10px',
                      borderRadius: '10px',
                      backgroundColor: message.sender === 'user' ? '#d1e7dd' : '#d4e2d4',
                      alignSelf: message.sender === 'user' ? 'flex-end' : 'flex-start',
                      maxWidth: '80%',
                      wordWrap: 'break-word'
                    }}>
                      <strong>{message.sender}:</strong> {message.text}
                    </div>
                  ))}
                </div>
                <input type="text" onKeyDown={(e) => e.key === 'Enter' && handleSend(e.target.value)} />
              </div>
            </Paper>
          )}
        </Box>
      </FireNav>
    </Box>
  );
}