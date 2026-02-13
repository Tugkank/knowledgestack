const mongoose = require('mongoose');

const QuestionSchema = new mongoose.Schema({
    id: { type: Number, required: true, unique: true },
    category: { type: String, required: true },
    text_tr: { type: String, required: true },
    text_en: { type: String },
    answer: { type: String, required: true },
    wrong: [{ type: String }],
    difficulty: { type: Number, required: true, min: 1, max: 4 },
    time: { type: Number, default: 15 }
});

module.exports = mongoose.model('Question', QuestionSchema);
