# Kelime Ezberleme Oyunu (Vocabulary Memorization Game)

A .NET 8 MVC application for learning English vocabulary using the "6 Sefer Tekrar Prensibi" (6-Time Repetition Principle).

## Features

- User authentication (register, login, forgot password)
- Word management (add, edit, delete words with images and audio)
- Spaced repetition quiz system
- Progress tracking
- User settings
- Responsive design with Bootstrap
- Dark/Light theme support

## Prerequisites

- .NET 8 SDK
- SQLite (included in the project)
- Visual Studio 2022 or Visual Studio Code

## Setup Instructions

1. Clone the repository:
```bash
git clone [repository-url]
cd 6Memorize
```

2. Install dependencies:
```bash
dotnet restore
```

3. Create the database:
```bash
dotnet ef database update
```

4. Run the application:
```bash
dotnet run
```

5. Open your browser and navigate to:
```
https://localhost:5001
```

## Project Structure

- `Controllers/` - MVC controllers
- `Models/` - Entity models
- `Views/` - Razor views
- `Data/` - Database context and migrations
- `wwwroot/` - Static files (CSS, JS, images)

## Database Schema

- Users
  - UserID (PK)
  - UserName
  - Password
  - Email
  - CreatedAt
  - LastLoginAt
  - IsActive

- Words
  - WordID (PK)
  - EngWordName
  - TurWordName
  - PicturePath
  - AudioPath
  - DifficultyLevel
  - CreatedAt

- WordSamples
  - WordSamplesID (PK)
  - WordID (FK)
  - Sample

- UserWordProgress
  - ID (PK)
  - UserID (FK)
  - WordID (FK)
  - CurrentStep
  - NextDueDate
  - IsCompleted
  - LastAttemptDate
  - LastAttemptSuccess

- UserSettings
  - ID (PK)
  - UserID (FK)
  - WordsPerQuiz
  - ShowAudio
  - ShowImages
  - Theme

## Spaced Repetition Algorithm

The application implements a spaced repetition system with the following intervals:
1. Day 1
2. Day 7
3. Day 30
4. Day 90
5. Day 180
6. Day 365

After successfully completing all 6 repetitions, the word is marked as "known" and moved to a review pool.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details. 