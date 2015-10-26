using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PedestrianHeadAnnotator
{
    public enum InterpolationMode
    {
        Gaze = 0,
        //Body,
        //Head
    }

    class ViewModel : INotifyPropertyChanged
    {
        private State state = new NotReadyState();
        public State State
        {
            get { return state; }
            set
            {
                state = value;
                RaisePropertyChanged(nameof(Ready));
            }
        }

        private InterpolationMode interpolationMode = InterpolationMode.Gaze;
        public InterpolationMode InterpolationMode
        {
            get { return interpolationMode; }
            set
            {
                if (interpolationMode != value)
                {
                    interpolationMode = value;
                    RaisePropertyChanged(nameof(IsGazeInterpolation));
                }
            }
        }

        private int interpolationMin;
        public int InterpolationBegin
        {
            get { return interpolationMin; }
            set
            {
                if (interpolationMin != value)
                {
                    interpolationMin = value;
                    RaisePropertyChanged(nameof(InterpolationBegin));
                }
            }
        }

        private int interpolationMax;
        public int InterpolationEnd
        {
            get { return interpolationMax; }
            set
            {
                if (interpolationMax != value)
                {
                    interpolationMax = value;
                    RaisePropertyChanged(nameof(InterpolationEnd));
                }
            }
        }


        /// <summary> 画像の指定が完了しているか </summary>
        public bool Ready { get { return !(State is NotReadyState); } }
        /// <summary> gazeの補間中か </summary>
        public System.Windows.Visibility IsGazeInterpolation
        {
            get { return InterpolationMode == InterpolationMode.Gaze ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; }
        }

        private DetectedHead detectedHead = new DetectedHead();
        public DetectedHead DetectedHead
        {
            get { return detectedHead; }
            set
            {
                detectedHead = value;
            }
        }
        
        //private System.Windows.IInputElement displayArea;
        //public ViewModel() { }
        //public ViewModel(System.Windows.IInputElement displayArea_):base()
        //{
        //    displayArea = displayArea_;
        //}

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            var d = PropertyChanged;
            if (d != null)
                d(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
