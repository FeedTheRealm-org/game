using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement("GoldSlider")]
public partial class GoldSlider : Slider
{
    private Label _valueLabel;

    public GoldSlider()
        : base()
    {
        Init();
    }

    public GoldSlider(
        string label,
        float start,
        float end,
        SliderDirection direction = SliderDirection.Horizontal,
        float pageSize = 0f
    )
        : base(label, start, end, direction, pageSize)
    {
        Init();
    }

    private void Init()
    {
        AddToClassList("setting-slider");

        _valueLabel = new Label();
        _valueLabel.AddToClassList("slider-value-label");
        Add(_valueLabel);

        RegisterCallback<ChangeEvent<float>>(evt => UpdateLabel(evt.newValue));
        RegisterCallback<AttachToPanelEvent>(evt => UpdateLabel(value));

        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.Center;
    }

    private void UpdateLabel(float val)
    {
        if (_valueLabel == null)
            return;
        int pct = Mathf.RoundToInt(val * 100f);
        _valueLabel.text = $"{pct}%";
    }
}
