const mongoose = require('mongoose');

const UserSchema = new mongoose.Schema({
    userId: { type: String, required: true, unique: true }, // Device ID or Play Games ID
    level: { type: Number, default: 1 },
    totalScore: { type: Number, default: 0 },
    servedQuestions: [{ type: Number }], // List of Question IDs solved
    lastLogin: { type: Date, default: Date.now }
});

module.exports = mongoose.model('User', UserSchema);
