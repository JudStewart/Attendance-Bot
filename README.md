# Attendance-Bot
A Discord.NET bot I created to make fun of my friends for saying that they would be online at a specific time and then being late.

## Implemented Commands
- **!test [message]**: Repeats the text written after "!test". Used for testing purposes.
- **!schedule [@user] [time]**: Schedules the user pinged for the given time. If the user is in the designated voice channel at the scheduled time, they will be marked present. Otherwise, they will be pinged by the bot and marked as late.
