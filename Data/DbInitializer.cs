using _6Memorize.Models;
using Microsoft.EntityFrameworkCore;

namespace _6Memorize.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // Check if we already have words
            if (context.Words.Any())
            {
                return; // DB has been seeded
            }

            // First, add all words
            var words = new Word[]
            {
                // Food & Drinks Category
                new Word { EngWordName = "Apple", TurWordName = "Elma", Category = "Food & Drinks", PicturePath = "/images/apple.jpg" },
                new Word { EngWordName = "Orange", TurWordName = "Portakal", Category = "Food & Drinks", PicturePath = "/images/orange.png" },
                new Word { EngWordName = "Bread", TurWordName = "Ekmek", Category = "Food & Drinks", PicturePath = "/images/bread.jpg" },
                new Word { EngWordName = "Coffee", TurWordName = "Kahve", Category = "Food & Drinks", PicturePath = "/images/coffee.jpg" },
                new Word { EngWordName = "Water", TurWordName = "Su", Category = "Food & Drinks", PicturePath = "/images/water.jpg" },

                // Animals Category
                new Word { EngWordName = "Cat", TurWordName = "Kedi", Category = "Animals", PicturePath = "/images/cat.jpg" },
                new Word { EngWordName = "Dog", TurWordName = "Köpek", Category = "Animals", PicturePath = "/images/dog.jpeg" },
                new Word { EngWordName = "Elephant", TurWordName = "Fil", Category = "Animals", PicturePath = "/images/elephant.jpg" },
                new Word { EngWordName = "Bird", TurWordName = "Kuş", Category = "Animals", PicturePath = "/images/bird.jpg" },
                new Word { EngWordName = "Fish", TurWordName = "Balık", Category = "Animals", PicturePath = "/images/fish.jpg" },

                // Nature Category
                new Word { EngWordName = "Tree", TurWordName = "Ağaç", Category = "Nature", PicturePath = "/images/tree.jpg" },
                new Word { EngWordName = "Flower", TurWordName = "Çiçek", Category = "Nature", PicturePath = "/images/flower.jpg" },
                new Word { EngWordName = "Mountain", TurWordName = "Dağ", Category = "Nature", PicturePath = "/images/mountain.jpg" },
                new Word { EngWordName = "River", TurWordName = "Nehir", Category = "Nature", PicturePath = "/images/river.jpg" },
                new Word { EngWordName = "Sun", TurWordName = "Güneş", Category = "Nature", PicturePath = "/images/sun.jpg" },

                // Education Category
                new Word { EngWordName = "Book", TurWordName = "Kitap", Category = "Education", PicturePath = "/images/book.jpg" },
                new Word { EngWordName = "Notebook", TurWordName = "Defter", Category = "Education", PicturePath = "/images/notebook.jpg" },
                new Word { EngWordName = "Pencil", TurWordName = "Kalem", Category = "Education", PicturePath = "/images/pencil.jpg" },
                new Word { EngWordName = "School", TurWordName = "Okul", Category = "Education", PicturePath = "/images/school.jpg" },
                new Word { EngWordName = "Teacher", TurWordName = "Öğretmen", Category = "Education", PicturePath = "/images/teacher.jpg" },

                // Music Category
                new Word { EngWordName = "Guitar", TurWordName = "Gitar", Category = "Music", PicturePath = "/images/guitar.png" },
                new Word { EngWordName = "Piano", TurWordName = "Piyano", Category = "Music", PicturePath = "/images/piano.png" },
                new Word { EngWordName = "Violin", TurWordName = "Keman", Category = "Music", PicturePath = "/images/violin.jpg" },
                new Word { EngWordName = "Drum", TurWordName = "Davul", Category = "Music", PicturePath = "/images/drum.jpg" },
                new Word { EngWordName = "Flute", TurWordName = "Flüt", Category = "Music", PicturePath = "/images/flute.jpg" }
            };

            context.Words.AddRange(words);
            context.SaveChanges();

            // Now add word samples
            var wordSamples = new List<WordSample>
            {
                // Food & Drinks samples
                new WordSample { WordID = words[0].WordID, Sample = "I eat an apple every day." },
                new WordSample { WordID = words[0].WordID, Sample = "The apple is red and juicy." },
                new WordSample { WordID = words[1].WordID, Sample = "I eat an orange every morning." },
                new WordSample { WordID = words[1].WordID, Sample = "The orange is sweet and juicy." },
                new WordSample { WordID = words[2].WordID, Sample = "I buy fresh bread every day." },
                new WordSample { WordID = words[2].WordID, Sample = "The bread smells delicious." },
                new WordSample { WordID = words[3].WordID, Sample = "I drink coffee in the morning." },
                new WordSample { WordID = words[3].WordID, Sample = "Would you like some coffee?" },
                new WordSample { WordID = words[4].WordID, Sample = "I drink water every day." },
                new WordSample { WordID = words[4].WordID, Sample = "The water in the lake is very clear." },

                // Animals samples
                new WordSample { WordID = words[5].WordID, Sample = "The cat is sleeping on the sofa." },
                new WordSample { WordID = words[5].WordID, Sample = "My cat likes to play with yarn." },
                new WordSample { WordID = words[6].WordID, Sample = "The dog is barking at the mailman." },
                new WordSample { WordID = words[6].WordID, Sample = "I take my dog for a walk every morning." },
                new WordSample { WordID = words[7].WordID, Sample = "The elephant is the largest land animal." },
                new WordSample { WordID = words[7].WordID, Sample = "Elephants have very long trunks." },
                new WordSample { WordID = words[8].WordID, Sample = "The bird is singing in the tree." },
                new WordSample { WordID = words[8].WordID, Sample = "I saw a beautiful bird in the garden." },
                new WordSample { WordID = words[9].WordID, Sample = "The fish is swimming in the aquarium." },
                new WordSample { WordID = words[9].WordID, Sample = "I like to watch the fish in the pond." },

                // Nature samples
                new WordSample { WordID = words[10].WordID, Sample = "The tree provides shade in summer." },
                new WordSample { WordID = words[10].WordID, Sample = "Birds are singing in the tree." },
                new WordSample { WordID = words[11].WordID, Sample = "The garden is full of beautiful flowers." },
                new WordSample { WordID = words[11].WordID, Sample = "She received a bouquet of flowers." },
                new WordSample { WordID = words[12].WordID, Sample = "The mountain is covered with snow." },
                new WordSample { WordID = words[12].WordID, Sample = "They climbed the mountain last summer." },
                new WordSample { WordID = words[13].WordID, Sample = "The river flows through the city." },
                new WordSample { WordID = words[13].WordID, Sample = "We went swimming in the river." },
                new WordSample { WordID = words[14].WordID, Sample = "The sun is very bright today." },
                new WordSample { WordID = words[14].WordID, Sample = "We enjoy the warm sun in summer." },

                // Education samples
                new WordSample { WordID = words[15].WordID, Sample = "I love reading books." },
                new WordSample { WordID = words[15].WordID, Sample = "This book is very interesting." },
                new WordSample { WordID = words[16].WordID, Sample = "I write my notes in a notebook." },
                new WordSample { WordID = words[16].WordID, Sample = "She bought a new notebook for school." },
                new WordSample { WordID = words[17].WordID, Sample = "I need a pencil to write." },
                new WordSample { WordID = words[17].WordID, Sample = "Can I borrow your pencil?" },
                new WordSample { WordID = words[18].WordID, Sample = "The school is closed for summer." },
                new WordSample { WordID = words[18].WordID, Sample = "I walk to school every day." },
                new WordSample { WordID = words[19].WordID, Sample = "The teacher is explaining the lesson." },
                new WordSample { WordID = words[19].WordID, Sample = "Our teacher is very kind." },

                // Music samples
                new WordSample { WordID = words[20].WordID, Sample = "He plays the guitar very well." },
                new WordSample { WordID = words[20].WordID, Sample = "I'm learning to play the guitar." },
                new WordSample { WordID = words[21].WordID, Sample = "She plays the piano beautifully." },
                new WordSample { WordID = words[21].WordID, Sample = "The piano needs to be tuned." },
                new WordSample { WordID = words[22].WordID, Sample = "He plays the violin in an orchestra." },
                new WordSample { WordID = words[22].WordID, Sample = "The violin makes beautiful music." },
                new WordSample { WordID = words[23].WordID, Sample = "The drummer is very talented." },
                new WordSample { WordID = words[23].WordID, Sample = "I can hear the drums from here." },
                new WordSample { WordID = words[24].WordID, Sample = "She plays the flute in the band." },
                new WordSample { WordID = words[24].WordID, Sample = "The flute has a beautiful sound." }
            };

            context.WordSamples.AddRange(wordSamples);
            context.SaveChanges();
        }
    }
}