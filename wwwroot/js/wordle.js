// Wordle oyun mantığını yöneten kod
$(document).ready(function() {
    // Game variables
    let word = "";
    let meaning = "";
    let WORD_LENGTH = 0;
    const MAX_GUESSES = 6;
    let currentGuess = 0;
    let currentPosition = 0;
    let guesses = [];
    let gameOver = false;

    // Oyunu başlat
    function initGame(config) {
        word = config.word.toLowerCase();
        meaning = config.meaning;
        WORD_LENGTH = config.wordLength;

        // Oyun değişkenlerini ayarla
        currentGuess = 0;
        currentPosition = 0;
        guesses = Array(MAX_GUESSES).fill().map(() => Array(WORD_LENGTH).fill(''));
        gameOver = false;

        createBoard();
        $("#guess-input").focus();
    }

    // Oyun tahtasını oluştur
    function createBoard() {
        const boardContainer = $("#board-container");
        boardContainer.empty();

        for (let i = 0; i < MAX_GUESSES; i++) {
            const row = $("<div>").addClass("guess-row d-flex mb-2");

            for (let j = 0; j < WORD_LENGTH; j++) {
                const cell = $("<div>").addClass("cell border d-flex justify-content-center align-items-center mx-1")
                    .css({
                        "width": "50px",
                        "height": "50px",
                        "font-size": "24px",
                        "font-weight": "bold",
                        "border-radius": "8px",
                        "transition": "all 0.3s ease"
                    });
                row.append(cell);
            }

            boardContainer.append(row);
        }

        // İlk satırı aktif olarak işaretle
        $(".guess-row").first().find(".cell").addClass("border-primary");
        $("#attempts-counter").text(`1/${MAX_GUESSES}`);
    }

    // Türkçe karakterleri İngilizce eşdeğerlerine dönüştürme
    function normalizeEnglishInput(text) {
        const charMap = {
            'ı': 'i', 'ğ': 'g', 'ü': 'u', 'ş': 's', 'ö': 'o', 'ç': 'c',
            'İ': 'I', 'Ğ': 'G', 'Ü': 'U', 'Ş': 'S', 'Ö': 'O', 'Ç': 'C'
        };

        return text.split('').map(char => charMap[char] || char).join('');
    }

    // Tahmini kontrol et
    function checkGuess() {
        const input = $("#guess-input");
        const rawGuess = input.val().toLowerCase();

        const guess = normalizeEnglishInput(rawGuess);
        if (guess.length !== WORD_LENGTH) {
            showMessage(`Please enter a ${WORD_LENGTH}-letter word!`, "warning");
            return;
        }

        // Harfleri kontrol et ve renkleri güncelle
        const row = $(".guess-row").eq(currentGuess);
        const letterCount = {};

        // Kelimede her harften kaç tane olduğunu say
        for (let i = 0; i < word.length; i++) {
            letterCount[word[i]] = (letterCount[word[i]] || 0) + 1;
        }

        // İlk olarak tam eşleşmeleri kontrol et ve letterCount'tan düş
        for (let i = 0; i < WORD_LENGTH; i++) {
            if (guess[i] === word[i]) {
                letterCount[guess[i]]--;
            }
        }

        // Şimdi tüm harfleri kontrol et ve animasyonla renkleri güncelle
        for (let i = 0; i < WORD_LENGTH; i++) {
            const cell = row.children().eq(i);
            const letter = guess[i];

            // Döndürme animasyonu
            setTimeout(() => {
                cell.css("transform", "rotateX(90deg)");

                setTimeout(() => {
                    if (letter === word[i]) {
                        // Tam eşleşme - yeşil
                        cell.css("background-color", "#198754").css("color", "white")
                            .css("border-color", "#198754");
                    } else if (word.includes(letter) && letterCount[letter] > 0) {
                        // Kelimede var ama yanlış pozisyon - sarı
                        cell.css("background-color", "#ffc107").css("color", "black")
                            .css("border-color", "#ffc107");
                        letterCount[letter]--; // Bu harf için sayıyı azalt
                    } else {
                        // Kelimede yok veya tüm oluşumlar kullanıldı - gri
                        cell.css("background-color", "#6c757d").css("color", "white")
                            .css("border-color", "#6c757d");
                    }

                    // Döndürmeyi geri al
                    cell.css("transform", "rotateX(0deg)");
                }, 250);
            }, i * 200);
        }

        // Giriş kutusunu temizle
        input.val("");

        // Sonuçları kontrol et
        if (guess === word) {
            setTimeout(() => {
                gameOver = true;
                showResult(true);
            }, WORD_LENGTH * 200 + 300);
        } else {
            currentGuess++;

            if (currentGuess >= MAX_GUESSES) {
                setTimeout(() => {
                    gameOver = true;
                    showResult(false);
                }, WORD_LENGTH * 200 + 300);
            } else {
                // Aktif satırı güncelle
                setTimeout(() => {
                    $(".guess-row").removeClass("active").find(".cell").removeClass("border-primary");
                    $(".guess-row").eq(currentGuess).addClass("active").find(".cell").addClass("border-primary");
                    $("#attempts-counter").text(`${currentGuess + 1}/${MAX_GUESSES}`);
                }, WORD_LENGTH * 200 + 300);
            }
        }
    }

    // Sonuçları göster
    function showResult(win) {
        const resultContainer = $("#result-container");
        const resultMessage = $("#result-message");
        const wordMeaning = $("#word-meaning");

        if (win) {
            resultMessage.html('<i class="fas fa-trophy text-warning me-2"></i> Congratulations! You found the word.');
        } else {
            resultMessage.html('<i class="fas fa-times-circle text-danger me-2"></i> Sorry, you didn\'t find the correct word.');
            resultMessage.append(`<div class="mt-2">Correct answer: <strong>${word.toUpperCase()}</strong></div>`);
        }

        wordMeaning.html(`<i class="fas fa-language me-2"></i> Word meaning: <strong>${meaning}</strong>`);
        resultContainer.fadeIn();

        // Mesajı gizle
        $("#message").hide();
    }

    // Mesaj göster
    function showMessage(text, type) {
        const message = $("#message");
        message.removeClass("alert-success alert-danger alert-warning alert-info")
            .addClass("alert-" + type)
            .html(`<i class="fas ${type === 'warning' ? 'fa-exclamation-triangle' : 'fa-info-circle'} me-2"></i> ${text}`)
            .fadeIn();

        setTimeout(() => {
            message.fadeOut();
        }, 3000);
    }

    // Giriş değişikliklerini yakala
    $(document).on("input", "#guess-input", function() {
        const rawInput = $(this).val().toLowerCase();
        const input = normalizeEnglishInput(rawInput);

        // Giriş içeriğini güncelle
        guesses[currentGuess] = Array.from(input.padEnd(WORD_LENGTH, ''));

        // Görüntüyü güncelle
        const row = $(".guess-row").eq(currentGuess);

        for (let i = 0; i < WORD_LENGTH; i++) {
            const cell = row.children().eq(i);
            cell.text(i < input.length ? input[i].toUpperCase() : '');
        }

        currentPosition = input.length;
    });

    // Tahmin butonuna tıklama
    $(document).on("click", "#guess-button", function() {
        if (!gameOver) {
            checkGuess();
        }
    });

    // Enter tuşuna basma
    $(document).on("keypress", "#guess-input", function(e) {
        if (e.which === 13 && !gameOver) {
            checkGuess();
        }
    });

    // WordleGame'i global olarak erişilebilir yap
    window.WordleGame = {
        init: initGame
    };
});