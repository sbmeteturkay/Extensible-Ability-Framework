using UnityEngine;
using UnityEngine.UI;

/*
Radial Layout Group by Just a Pixel (Danny Goodayle) - http://www.justapixel.co.uk
Updated for Unity 6 compatibility
*/
public class RadialLayout : LayoutGroup {
    public float fDistance;
    [Range(0f, 360f)]
    public float MinAngle, MaxAngle, StartAngle;

    protected override void OnEnable() { base.OnEnable(); CalculateRadial(); }

    public override void SetLayoutHorizontal() => CalculateRadial();
    public override void SetLayoutVertical() => CalculateRadial();

    public override void CalculateLayoutInputVertical() { }
    public override void CalculateLayoutInputHorizontal() { }

    #if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        // Unity 6'da SendMessage hatasını önlemek için işlemi kuyruğa alıyoruz
        UnityEditor.EditorApplication.delayCall += () => {
            if (this != null) CalculateRadial();
        };
    }
    #endif

    void CalculateRadial()
    {
        m_Tracker.Clear();
        if (transform.childCount == 0)
            return;

        // Kaç çocuk varsa ona göre açı aralığını hesapla
        int activeChildCount = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeInHierarchy) activeChildCount++;
        }

        if (activeChildCount == 0) return;

        float fOffsetAngle = (activeChildCount > 1) ? (MaxAngle - MinAngle) / (activeChildCount - 1) : 0;
        
        float fAngle = StartAngle;
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = (RectTransform)transform.GetChild(i);
            if (child != null && child.gameObject.activeInHierarchy)
            {
                // Tracker ile editör kontrolünü kısıtlıyoruz
                m_Tracker.Add(this, child, 
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.AnchoredPosition |
                    DrivenTransformProperties.Pivot);

                // Matematiksel pozisyon hesaplama
                Vector3 vPos = new Vector3(Mathf.Cos(fAngle * Mathf.Deg2Rad), Mathf.Sin(fAngle * Mathf.Deg2Rad), 0);
                child.localPosition = vPos * fDistance;

                // Anchor ve pivotları merkeze sabitleme
                child.anchorMin = child.anchorMax = child.pivot = new Vector2(0.5f, 0.5f);
                
                fAngle += fOffsetAngle;
            }
        }
    }
}