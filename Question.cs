using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Requests;

namespace TelegramPDDBot
{
    internal class Question
    {
        public string QuestionId { get; set; }
        public string ImagePath { get; set; }
        public string GivenQuestion { get; set; }
        public JArray Answers { get; set; }  
        public string AnswerTip { get; set; }
        public string CorrectAnswer { get; set; }

        public Question(string questionId, string imagePath, string question, JArray answers, string answerTip, string correctAnswer) 
        {
            QuestionId = questionId;
            ImagePath = imagePath;
            GivenQuestion = question;
            Answers = answers;
            AnswerTip = answerTip;
            CorrectAnswer = correctAnswer;
        }
    }
}
