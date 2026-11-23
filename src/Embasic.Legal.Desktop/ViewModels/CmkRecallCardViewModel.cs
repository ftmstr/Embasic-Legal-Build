using System.Windows.Input;
using Embasic.Legal.Core;

namespace Embasic.Legal.Desktop.ViewModels;

public sealed class CmkRecallCardViewModel : ObservableViewModel
{
    private bool _revealed;
    private CmkRecallRating _rating;
    public string Question { get; }
    public string Answer { get; }
    public string Reference { get; }
    public bool IsRevealed { get => _revealed; private set { _revealed = value; Changed(); } }
    public string RatingLabel => _rating switch
    {
        CmkRecallRating.Remembered => "Özdeğerlendirme: Hatırladım",
        CmkRecallRating.Review => "Özdeğerlendirme: Tekrar çalışacağım",
        _ => "Önce kendi cevabınızı düşünün, sonra cevabı açın."
    };
    public ICommand RevealCommand { get; }
    public ICommand RememberCommand { get; }
    public ICommand ReviewCommand { get; }
    public CmkRecallCardViewModel(CmkRecallCard card, string topicKey, int index,
        CmkStudySession session, Action notify)
    {
        Question = card.Question; Answer = card.Answer; Reference = card.ArticleLocator;
        _revealed = session.IsRevealed(topicKey, index);
        _rating = session.Rating(topicKey, index);
        RevealCommand = new RelayCommand(_ => { session.Reveal(topicKey, index); IsRevealed = true; });
        void Rate(CmkRecallRating rating)
        {
            if (!IsRevealed) return;
            session.Rate(topicKey, index, rating); _rating = rating; Changed(nameof(RatingLabel)); notify();
        }
        RememberCommand = new RelayCommand(_ => Rate(CmkRecallRating.Remembered));
        ReviewCommand = new RelayCommand(_ => Rate(CmkRecallRating.Review));
    }
}

public sealed record CmkFlowChoice(int Order, string Stage, bool Active);
public sealed record CmkBranchChoice(int Index, string Text, bool Selected);
