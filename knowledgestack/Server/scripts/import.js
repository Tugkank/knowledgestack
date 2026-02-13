const mongoose = require('mongoose');
const fs = require('fs');
const path = require('path');
const Question = require('../models/Question');
require('dotenv').config({ path: path.join(__dirname, '../.env') });

const MONGO_URI = process.env.MONGO_URI || 'mongodb://localhost:27017/knowledgestack';

mongoose.connect(MONGO_URI)
    .then(() => {
        console.log('MongoDB Connected for Import...');
        importData();
    })
    .catch(err => console.log(err));

async function importData() {
    try {
        // Read JSON from Unity Resources
        const jsonPath = path.join(__dirname, '../../Assets/Resources/questions.json');
        const rawData = fs.readFileSync(jsonPath, 'utf-8');
        const data = JSON.parse(rawData);

        // Clear existing questions to avoid duplicates? Optional.
        await Question.deleteMany({});
        console.log('Cleared existing questions.');

        // Import
        await Question.insertMany(data.questions);
        console.log(`Successfully imported ${data.questions.length} questions!`);

        process.exit();
    } catch (err) {
        console.error('Import Failed:', err);
        process.exit(1);
    }
}
