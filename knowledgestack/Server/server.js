require('dotenv').config();
const express = require('express');
const mongoose = require('mongoose');
const bodyParser = require('body-parser');
const cors = require('cors');

const User = require('./models/User');
const Question = require('./models/Question');

const app = express();
app.use(cors());
app.use(bodyParser.json());

// MongoDB Connection
const MONGO_URI = process.env.MONGO_URI || 'mongodb://localhost:27017/knowledgestack';
mongoose.connect(MONGO_URI)
    .then(() => console.log('MongoDB Connected'))
    .catch(err => console.log(err));

// --- ROUTES ---

// 1. LOGIN / SYNC USER
app.post('/api/auth/login', async (req, res) => {
    const { userId } = req.body;
    if (!userId) return res.status(400).json({ error: 'UserID required' });

    try {
        let user = await User.findOne({ userId });
        if (!user) {
            user = new User({ userId });
            await user.save();
        }
        res.json(user);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// 2. GET QUESTIONS (Filtered by Level Distribution done inside Unity or here?)
// For simplicity, let's serve ALL questions once and let Unity handle logic, 
// OR serve specific batch. Let's serve ALL for local caching performance in game start.
app.get('/api/game/questions', async (req, res) => {
    try {
        const questions = await Question.find({});
        res.json({ questions }); // Matching Unity JSON format
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// 3. SYNC PROGRESS
app.post('/api/game/sync', async (req, res) => {
    const { userId, level, score, solvedQuestionId } = req.body;

    try {
        const user = await User.findOne({ userId });
        if (!user) return res.status(404).json({ error: 'User not found' });

        if (level) user.level = level;
        if (score) user.totalScore = score; // Or increment? Usually sync total is safer

        if (solvedQuestionId && !user.servedQuestions.includes(solvedQuestionId)) {
            user.servedQuestions.push(solvedQuestionId);
        }

        user.lastLogin = Date.now();
        await user.save();

        res.json({ success: true, user });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`Server running on port ${PORT}`));
