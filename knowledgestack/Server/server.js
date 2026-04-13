require('dotenv').config();
const express = require('express');
const mongoose = require('mongoose');
const bodyParser = require('body-parser');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');

const User = require('./models/User');
const Question = require('./models/Question');

const app = express();

// 1. HTTP Security Headers
// contentSecurityPolicy kapalı (false) yapıldı çünkü admin.html içindeki JavaScript'in çalışmasını engelliyor.
app.use(helmet({ contentSecurityPolicy: false }));

// 2. Cross Origin Resource Sharing
app.use(cors());

// 3. Body Parser with Size Limits (Against Large Payload Attacks)
app.use(bodyParser.json({ limit: '50kb' }));

// SADECE BUNU EKLEYİN: HTML dosyalarımızı doğrudan sunar
const path = require('path');
app.use(express.static(__dirname));

// 4. Rate Limiting (Against DDoS / Brute Force)
const apiLimiter = rateLimit({
    windowMs: 15 * 60 * 1000, // 15 minutes
    max: 200, // Limit each IP to 200 requests per windowMs
    message: 'Too many requests from this IP, please try again later.'
});
app.use('/api', apiLimiter);

// 5. Basic API Key Middleware (Against Unauthorized Access)
const APP_SECRET = process.env.APP_SECRET || 'knowledge_stack_secret_2024';
const apiKeyAuth = (req, res, next) => {
    const apiKey = req.headers['x-api-key'];
    if (!apiKey || apiKey !== APP_SECRET) {
        return res.status(401).json({ error: 'Unauthorized access. Invalid API Key.' });
    }
    next();
};

// MongoDB Connection
const MONGO_URI = process.env.MONGO_URI || 'mongodb://localhost:27017/knowledgestack';
mongoose.connect(MONGO_URI)
    .then(() => console.log('MongoDB Connected'))
    .catch(err => console.log('MongoDB Connection Error:', err));

// --- ROUTES ---

// --- ADMIN ROUTES ---
const ADMIN_USER = process.env.ADMIN_USER || 'admin';
const ADMIN_PASS = process.env.ADMIN_PASS || 'KnowledgeAdmin2024';
const ADMIN_TOKEN = 'secure_admin_token_2024_temp'; // Simple temporary token

// Admin Middleware
const adminAuth = (req, res, next) => {
    const token = req.headers['x-admin-token'];
    if (!token || token !== ADMIN_TOKEN) {
        return res.status(401).json({ error: 'Unauthorized access.' });
    }
    next();
};

// Admin Login Route
app.post('/api/admin/login', (req, res) => {
    const { username, password } = req.body;
    if (username === ADMIN_USER && password === ADMIN_PASS) {
        res.json({ success: true, token: ADMIN_TOKEN });
    } else {
        res.status(401).json({ error: 'Invalid credentials.' });
    }
});

// Admin Get Users Route (Leaderboard)
app.get('/api/admin/users', adminAuth, async (req, res) => {
    try {
        const users = await User.find({}).sort({ level: -1, totalScore: -1 });
        res.json({ users });
    } catch (err) {
        res.status(500).json({ error: 'Server Error' });
    }
});

// 1. LOGIN / SYNC USER
app.post('/api/auth/login', apiKeyAuth, async (req, res) => {
    // SECURITY: Force string conversion to prevent NoSQL Injection (e.g., passing {$gt: ""} )
    const userId = String(req.body.userId);

    if (!userId || userId === 'undefined') {
        return res.status(400).json({ error: 'UserID required' });
    }

    try {
        let user = await User.findOne({ userId: userId });
        if (!user) {
            user = new User({ userId: userId });
            await user.save();
        }
        res.json(user);
    } catch (err) {
        res.status(500).json({ error: 'Server Error' });
    }
});

// 2. GET QUESTIONS
app.get('/api/game/questions', apiKeyAuth, async (req, res) => {
    try {
        const questions = await Question.find({});
        res.json({ questions });
    } catch (err) {
        res.status(500).json({ error: 'Server Error' });
    }
});

// 3. SYNC PROGRESS
app.post('/api/game/sync', apiKeyAuth, async (req, res) => {
    const userId = String(req.body.userId);
    const level = Number(req.body.level);
    const addedScore = Number(req.body.score);
    const solvedQuestionId = Number(req.body.solvedQuestionId);

    try {
        const user = await User.findOne({ userId });
        if (!user) return res.status(404).json({ error: 'User not found' });

        // SECURITY: Cheat Protection & Logic Validation
        if (!isNaN(level) && level > user.level) {
            // Allow larger level skips to support users who play offline and sync later
            // (Increased from +2 to +50 to prevent blocking legitimate offline progress)
            if (level <= user.level + 50) {
                user.level = level;
            }
        }

        if (!isNaN(addedScore) && addedScore > 0) {
            // Prevent ridiculous scores per request (e.g. max 500 per question/level)
            if (addedScore <= 1000) {
                user.totalScore += addedScore;
            }
        }

        if (!isNaN(solvedQuestionId) && !user.servedQuestions.includes(solvedQuestionId)) {
            user.servedQuestions.push(solvedQuestionId);
        }

        user.lastLogin = Date.now();
        await user.save();

        res.json({ success: true, user });
    } catch (err) {
        res.status(500).json({ error: 'Server Error' });
    }
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`Secure Server running on port ${PORT}`));
