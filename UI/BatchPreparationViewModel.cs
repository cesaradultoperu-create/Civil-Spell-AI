using System;
using System.ComponentModel;
using CivilSpellAI.Application;

namespace CivilSpellAI.UI
{
    public sealed class BatchPreparationViewModel : INotifyPropertyChanged
    {
        private int completedCount;
        private bool isCancelling;

        public BatchPreparationViewModel(int totalCount)
        {
            if (totalCount < 0)
                throw new ArgumentOutOfRangeException("totalCount");

            TotalCount = totalCount;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int TotalCount { get; private set; }

        public int CompletedCount
        {
            get { return completedCount; }
            private set
            {
                if (completedCount == value)
                    return;

                completedCount = value;
                RaisePropertyChanged("CompletedCount");
                RaisePropertyChanged("StatusText");
            }
        }

        public string StatusText
        {
            get
            {
                return IsCancelling
                    ? "Cancelando la revisión…"
                    : string.Format(
                        "Analizando {0} de {1} texto(s)…",
                        CompletedCount,
                        TotalCount);
            }
        }

        public bool IsCancelling
        {
            get { return isCancelling; }
            private set
            {
                if (isCancelling == value)
                    return;

                isCancelling = value;
                RaisePropertyChanged("IsCancelling");
                RaisePropertyChanged("CanCancel");
                RaisePropertyChanged("StatusText");
            }
        }

        public bool CanCancel
        {
            get { return !IsCancelling; }
        }

        public void Report(BatchReviewProgress progress)
        {
            if (progress == null || IsCancelling)
                return;

            CompletedCount = progress.CompletedCount;
        }

        public void BeginCancel()
        {
            IsCancelling = true;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
