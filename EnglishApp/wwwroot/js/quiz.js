// Sayfa yüklendiğinde çalışacak kod
$(document).ready(function () {
    let currentQuestion = 0;
    let wordProcesses = [];  // Bu değer HTML'den gelecek
    let completedWords = 0;
    let totalWords = 0;
    let showAudio = true;    // Bu değer HTML'den gelecek
    let showImages = true;   // Bu değer HTML'den gelecek

    // Quiz uygulamasını başlat
    function initQuiz(config) {
        wordProcesses = config.wordProcesses;
        totalWords = wordProcesses.length;
        showAudio = config.showAudio;
        showImages = config.showImages;

        // İlk soruyu yükle
        if (wordProcesses.length > 0) {
            loadQuestion(wordProcesses[currentQuestion]);
            updateProgress();
        }
    }

    // Sonraki soru butonu tıklandığında
    $(document).on('click', '#nextButton', function() {
        currentQuestion++;
        if (currentQuestion < wordProcesses.length) {
            loadQuestion(wordProcesses[currentQuestion]);
            updateProgress();
        } else {
            showCompletion();
        }
    });

    // Cevabı gönder
    $(document).on('click', '.submit-answer', function() {
        let answer = $('#answerInput').val().trim();
        let wordId = $(this).data('word-id');

        if (answer === '') {
            $('#answerInput').addClass('is-invalid');
            return;
        }

        $.ajax({
            url: '/Quiz/SubmitAnswer',
            type: 'POST',
            data: { wordId: wordId, answer: answer },
            success: function(response) {
                let feedbackDiv = $('.feedback');
                feedbackDiv.removeClass('d-none');

                if (response.success) {
                    // Doğru cevap
                    feedbackDiv.html(`
                        <div class="alert alert-success d-flex align-items-center">
                            <i class="fas fa-check-circle me-3 fa-lg"></i>
                            <div>
                                <strong>Correct!</strong> "${response.correctAnswer}" is the right answer.
                            </div>
                        </div>
                    `);
                    completedWords++;
                    updateProgress();
                } else {
                    // Yanlış cevap
                    feedbackDiv.html(`
                        <div class="alert alert-danger d-flex align-items-center">
                            <i class="fas fa-times-circle me-3 fa-lg"></i>
                            <div>
                                <strong>Incorrect!</strong> The correct answer is "${response.correctAnswer}".
                            </div>
                        </div>
                    `);
                }

                // Sonraki butonu göster
                $('#quizNavigation').removeClass('d-none');
                $('.submit-answer').addClass('d-none');
                $('#answerInput').attr('disabled', true);
            },
            error: function() {
                $('.feedback').removeClass('d-none').html(`
                    <div class="alert alert-danger">
                        <i class="fas fa-exclamation-circle me-2"></i>
                        An error occurred. Please try again.
                    </div>
                `);
            }
        });
    });

    // Enter tuşuna basıldığında cevap gönder
    $(document).on('keypress', '#answerInput', function(e) {
        if (e.which === 13 && !$('.submit-answer').hasClass('d-none')) {
            $('.submit-answer').click();
        }
    });

    // Soruyu yükle
    function loadQuestion(wordProcessId) {
        $('#quizContainer').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        `);

        $('#quizNavigation').addClass('d-none');

        $.ajax({
            url: '/Quiz/GetWordProcess',
            type: 'POST',
            data: { id: wordProcessId },
            success: function(response) {
                if (response.success) {
                    let template = $('#wordTemplate').html();
                    let content = $(template);

                    // Verileri doldur
                    content.find('.tur-word').text(response.turWordName);
                    content.find('.submit-answer').attr('data-word-id', response.wordId);

                    // Örnek cümle
                    let sample = response.samples && response.samples.length > 0
                        ? response.samples[0]
                        : "Example not available";

                    content.find('.word-sample').text(sample);

                    // Resim
                    if (showImages && response.picturePath) {
                        content.find('.word-picture').attr('src', response.picturePath);
                    } else {
                        content.find('.word-image').html('<div class="bg-light text-center p-5"><i class="fas fa-image fa-3x text-muted"></i></div>');
                    }

                    // Ses
                    if (showAudio && response.audioPath) {
                        content.find('.word-audio').attr('src', response.audioPath);
                    } else {
                        content.find('.audio-player').html('<div class="bg-light text-center p-3"><i class="fas fa-volume-mute fa-2x text-muted"></i></div>');
                    }

                    // Kategori bilgisi
                    $('#wordCategory').html(`<i class="fas fa-tag me-1"></i>${response.category || 'General'}`);

                    $('#quizContainer').html(content);
                    $('#answerInput').focus();

                    // Sayacı güncelle
                    $('#quizCounter').text(`${currentQuestion + 1} / ${totalWords}`);
                    $('#completedWords').text(`${completedWords} Completed`);
                    $('#remainingWords').text(`${totalWords - completedWords} Remaining`);
                } else {
                    $('#quizContainer').html("<div class='alert alert-danger'>Failed to load word.</div>");
                }
            },
            error: function() {
                $('#quizContainer').html("<div class='alert alert-danger'>Server error occurred.</div>");
            }
        });
    }

    // İlerleme çubuğunu güncelle
    function updateProgress() {
        let progress = ((currentQuestion / totalWords) * 100).toFixed(0);
        $('#quizProgress').css('width', progress + '%').attr('aria-valuenow', progress);

        $('#completedWords').text(`${completedWords} Completed`);
        $('#remainingWords').text(`${totalWords - completedWords} Remaining`);
    }

    // Quizi tamamlama ekranını göster
    function showCompletion() {
        $('#quizContainer').html(`
            <div class="text-center py-5">
                <i class="fas fa-trophy text-warning fa-5x mb-4"></i>
                <h3>Congratulations!</h3>
                <p class="lead mb-4">You have completed today's quiz.</p>
                <p class="text-muted">Correct answers: ${completedWords}/${totalWords}</p>

                <div class="mt-4">
                    <a href="/Quiz/Index" class="btn btn-primary btn-lg me-2">
                        <i class="fas fa-redo me-2"></i>New Quiz
                    </a>
                    <a href="/Quiz/Progress" class="btn btn-outline-primary btn-lg">
                        <i class="fas fa-chart-line me-2"></i>Progress
                    </a>
                </div>
            </div>
        `);

        $('#quizNavigation').addClass('d-none');
        $('#quizProgress').css('width', '100%').attr('aria-valuenow', 100);
    }

    // Quiz uygulamasını global olarak erişilebilir yap
    window.QuizApp = {
        init: initQuiz
    };
});