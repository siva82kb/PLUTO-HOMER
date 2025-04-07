#region Includes
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#endregion

namespace TS.DoubleSlider
{
    [RequireComponent(typeof(RectTransform))]
    public class DoubleSlider : MonoBehaviour
    {
        #region Variables

        [Header("References")]
        [SerializeField] private SingleSlider _sliderMin;
        [SerializeField] private SingleSlider _sliderMax;

        [SerializeField] private SingleSlider _sliderMinHoc;
        [SerializeField] private SingleSlider _sliderMaxHoc;
        [SerializeField] public Slider _currePostion;
        [SerializeField] private Slider _currePostionHoc;

        [SerializeField] private Text warningText;

        [SerializeField] private RectTransform _fillArea;
        [SerializeField] private RectTransform _fillAreahoc;

        [SerializeField] private RectTransform _fillAreahocleft;
        [SerializeField] private RectTransform oldROMArea;

        [SerializeField] private RectTransform oldROMAreaHoc;



        [Header("Configuration")]
        [SerializeField] private bool _setupOnStart;
        [SerializeField] private float _minValue;
        [SerializeField] private float _maxValue;

        [SerializeField] private float _minValueHoc;
        [SerializeField] private float _maxValueHoc;
        [SerializeField] private float _minDistance;
        [SerializeField] private bool _wholeNumbers;
        [SerializeField] private float _initialMinValue;
        [SerializeField] private float _initialMaxValue;

        [Header("Events")]
        public UnityEvent<float, float> OnValueChanged;

        public float minAng, maxAng;
        public bool UpdateMinMaxvalues;

        public PROMsceneHandler promSlider;
        public PROMsceneHandler aromSlider;

        public bool IsEnabled
        {
            get { return _sliderMax.IsEnabled && _sliderMin.IsEnabled; }
            set
            {
                _sliderMin.IsEnabled = value;
                _sliderMax.IsEnabled = value;
                _sliderMinHoc.IsEnabled = value;
                _sliderMaxHoc.IsEnabled = value;
            }
        }

        public float MinValue => _sliderMin.Value;
        public float MaxValue => _sliderMax.Value;

        public float MinValueHoc => _sliderMinHoc.Value;
        public float MaxValueHoc => _sliderMaxHoc.Value;

        public bool WholeNumbers
        {
            get => _wholeNumbers;
            set
            {
                _wholeNumbers = value;
                _sliderMin.WholeNumbers = _wholeNumbers;
                _sliderMax.WholeNumbers = _wholeNumbers;
            }
        }

        public bool IsDisabled { get; internal set; }

        private RectTransform _fillRect;

        private RectTransform _fillRectHoc;

        private RectTransform _fillRectHocleft;
        private RectTransform _oldROMRect;
        private RectTransform _oldROMRectHoc;
        private bool _isAromActive;
        private bool _isPromActive;


        #endregion

        private void Awake()
        {
            _fillRectHoc = _fillAreahoc.transform.GetChild(0).transform as RectTransform;
            _fillRect = _fillArea.transform.GetChild(0).transform as RectTransform;
            _oldROMRect = oldROMArea.transform.GetChild(0).transform as RectTransform;
            _oldROMRectHoc = oldROMAreaHoc.transform.GetChild(0).transform as RectTransform;
            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name) == 4)
            {

                _sliderMaxHoc.gameObject.SetActive(true);
                _sliderMinHoc.gameObject.SetActive(true);

                _maxValue = 0f;
                _minValue = -120f;
                _minValueHoc = 0f;
                _maxValueHoc = 120f;

                _currePostion.gameObject.SetActive(true);
                _currePostionHoc.gameObject.SetActive(true);
                _fillArea.gameObject.SetActive(true);
                _fillAreahoc.gameObject.SetActive(true);
            }
            else
            {

                _currePostionHoc.gameObject.SetActive(false);
                _sliderMaxHoc.gameObject.SetActive(false);
                _sliderMinHoc.gameObject.SetActive(false);
                _fillAreahoc.gameObject.SetActive(false);

            }
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name) == 4)
            {
                _currePostion.gameObject.SetActive(true);
                _currePostionHoc.gameObject.SetActive(true);
                Setup(_minValue, _maxValue, _initialMinValue, _initialMaxValue);
            }
            else
            {
                _currePostionHoc.gameObject.SetActive(false);
                _currePostion.gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            currentPositonUpdate();

            if (UpdateMinMaxvalues)
            {
                updateMinMaxVal();
            }


        }

        public void currentPositonUpdate()
        {

            _currePostion.value = PlutoComm.angle;
            _currePostionHoc.value = -PlutoComm.angle;


            float Currevalue = _currePostion.value;

        }

        public void updateMinMaxVal()
        {

            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name) == 4)
            {
                // Get the current position value
                float currentValue = -_currePostionHoc.value;

                //minAng = Mathf.Clamp(minAng, -90f, 0f);


                // Set the minSlider at minAng and allow it to move towards maxAng
                if (currentValue < minAng)
                {

                    minAng = Mathf.Clamp(currentValue, _minValue, 0f);
                    minAng = Mathf.Clamp(currentValue, _minValue, 0f);
                    _sliderMin.setSliderVal(minAng);

                    _sliderMinHoc.setSliderVal(-minAng);
                }

                // Allow the maxSlider to move within the range starting from minAng
                if (currentValue > maxAng)
                {
                    //maxAng = Mathf.Clamp(currentValue,  0f,-90f);
                    maxAng = Mathf.Clamp(currentValue, _minValue, 0f);
                    maxAng = Mathf.Clamp(currentValue, _minValue, 0f);
                    _sliderMax.setSliderVal(0f);

                    _sliderMaxHoc.setSliderVal(-maxAng);


                }

                // Ensure that the current position starts at minAng and goes to maxAng
                if (currentValue >= minAng && currentValue <= maxAng)
                {
                    _sliderMin.setSliderVal(minAng);
                    _sliderMax.setSliderVal(maxAng);

                    _sliderMinHoc.setSliderVal(-minAng);
                    _sliderMaxHoc.setSliderVal(-maxAng);

                    //_sliderMinHoc.setSliderVal(minAng);
                    //_sliderMaxHoc.setSliderVal(maxAng);
                }
            }

            else
            {
                if (_currePostion.value < minAng)
                {
                    minAng = Mathf.Clamp(_currePostion.value, _minValue, _maxValue);
                    _sliderMin.setSliderVal(minAng);
                }
                if (_currePostion.value > maxAng)
                {
                    maxAng = Mathf.Clamp(_currePostion.value, _minValue, _maxValue);
                    _sliderMax.setSliderVal(maxAng);
                }
            }
        }

        public void Setup(float minValue, float maxValue, float initialMinValue, float initialMaxValue)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _initialMinValue = initialMinValue;
            _initialMaxValue = initialMaxValue;

            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name) == 4)
            {
                _sliderMinHoc.Setup(-_initialMinValue, minValue, maxValue, MinValueChanged);
                _sliderMaxHoc.Setup(-_initialMaxValue, minValue, maxValue, MaxValueChanged);

                _sliderMin.Setup(_initialMinValue, -maxValue, -minValue, MinValueChanged);
                _sliderMax.Setup(_initialMaxValue, -maxValue, -minValue, MaxValueChanged);

                _currePostion.minValue = minValue;
                _currePostion.maxValue = maxValue;
                _currePostionHoc.minValue = minValue;
                _currePostionHoc.maxValue = maxValue;

            }
            else
            {

                _sliderMin.Setup(_initialMinValue, minValue, maxValue, MinValueChanged);
                _sliderMax.Setup(_initialMaxValue, minValue, maxValue, MaxValueChanged);


                MinValueChanged(_initialMinValue);
                MaxValueChanged(_initialMaxValue);


                _currePostion.minValue = minValue;
                _currePostion.maxValue = maxValue;


                OldROMRECT();
            }
        }

        public void startAssessment(float val)
        {
            minAng = val;
            maxAng = val;
            _initialMinValue = val;
            _initialMaxValue = val;

            _sliderMin.Setup(_initialMinValue, _minValue, _maxValue, MinValueChanged);
            _sliderMax.Setup(_initialMaxValue, _minValue, _maxValue, MaxValueChanged);

            MinValueChanged(val);
            MaxValueChanged(val);

            _currePostion.minValue = _minValue;
            _currePostion.maxValue = _maxValue;

            _oldROMRect.localScale = new Vector3(1, 5f, 1);


        }

        private void OldROMRECT()
        {

            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name) == 4)
            {

                float offset = ((MinValue - _minValue) / (_maxValue - _minValue)) * _fillAreahoc.rect.width;

                _oldROMRectHoc.offsetMin = new Vector2(offset, _fillRectHoc.offsetMin.y);
                offset = (1 - ((MaxValue - _minValue) / (_maxValue - _minValue))) * _fillAreahoc.rect.width;

                _oldROMRectHoc.offsetMax = new Vector2(-offset, _fillRectHoc.offsetMax.y);

                float offsetMin = ((-MaxValue - _minValue) / (_maxValue - _minValue)) * _fillArea.rect.width;
                _oldROMRect.offsetMin = new Vector2(offsetMin, _fillRect.offsetMin.y);

                float offsetMax = (1 - ((-MinValue - _minValue) / (_maxValue - _minValue))) * _fillArea.rect.width;
                _oldROMRect.offsetMax = new Vector2(-offsetMax, _fillRect.offsetMax.y);
            }
            else
            {


                float offset = ((MinValue - _minValue) / (_maxValue - _minValue)) * _fillArea.rect.width;

                _oldROMRect.offsetMin = new Vector2(offset, _fillRectHoc.offsetMin.y);
                offset = (1 - ((MaxValue - _minValue) / (_maxValue - _minValue))) * _fillArea.rect.width;

                _oldROMRect.offsetMax = new Vector2(-offset, _fillRectHoc.offsetMax.y);
            }

        }

        private void MinValueChanged(float value)
        {

            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name) == 4)
            {

                {
                    float offsetHoc = ((MinValue - _minValue) / (_maxValue - _minValue)) * _fillAreahoc.rect.width;
                    _fillRectHoc.offsetMin = new Vector2(offsetHoc, _fillRectHoc.offsetMin.y);



                    if ((MaxValue - value) < _minDistance)
                    {
                        _sliderMin.Value = MaxValue - _minDistance;
                    }

                    OnValueChanged.Invoke(MinValue, MaxValue);
                    _sliderMin.transform.SetAsLastSibling();
                }
                {

                    float offset = ((MinValue - _minValue) / (_maxValue - _minValue)) * _fillArea.rect.width;
                    _fillRect.offsetMin = new Vector2(offset, _fillRect.offsetMin.y);

                    if ((MaxValue - value) < _minDistance)
                    {
                        _sliderMin.Value = MaxValue - _minDistance;
                    }

                    OnValueChanged.Invoke(MinValue, -MaxValue);
                    _sliderMin.transform.SetAsLastSibling();
                }

            }
            else
            {

                float offset = ((MinValue - _minValue) / (_maxValue - _minValue)) * _fillArea.rect.width;
                _fillRect.offsetMin = new Vector2(offset, _fillRect.offsetMin.y);

                if ((MaxValue - value) < _minDistance)
                {
                    _sliderMin.Value = MaxValue - _minDistance;
                }

                OnValueChanged.Invoke(MinValue, MaxValue);
                _sliderMin.transform.SetAsLastSibling();
            }
        }

        private void MaxValueChanged(float value)
        {
            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name) == 4)
            {
                {

                    float offsetHoc = (1 - ((MaxValue - _minValue) / (_maxValue - _minValue))) * _fillAreahoc.rect.width;
                    _fillRectHoc.offsetMax = new Vector2(-offsetHoc, _fillRectHoc.offsetMax.y);

                    if ((value - MinValue) < _minDistance)
                    {
                        _sliderMax.Value = MinValue + _minDistance;
                    }

                    OnValueChanged.Invoke(MinValue, MaxValue);
                    _sliderMax.transform.SetAsLastSibling();
                }
                float offset = (1 - ((MaxValue - _minValue) / (_maxValue - _minValue))) * _fillArea.rect.width;
                _fillRect.offsetMax = new Vector2(-offset, _fillRect.offsetMax.y);

                if ((value - MinValue) < _minDistance)
                {
                    _sliderMax.Value = MinValue + _minDistance;
                }

                OnValueChanged.Invoke(MinValue, MaxValue);
                _sliderMax.transform.SetAsLastSibling();

            }
            else
            {
                float offset = (1 - ((MaxValue - _minValue) / (_maxValue - _minValue))) * _fillArea.rect.width;
                _fillRect.offsetMax = new Vector2(-offset, _fillRect.offsetMax.y);

                if ((value - MinValue) < _minDistance)
                {
                    _sliderMax.Value = MinValue + _minDistance;
                }

                OnValueChanged.Invoke(MinValue, MaxValue);
                _sliderMax.transform.SetAsLastSibling();

            }
        }


    }


}

