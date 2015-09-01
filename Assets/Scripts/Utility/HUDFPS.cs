using UnityEngine;
using System.Collections;
using System;
public class HUDFPS : SingletonMonoBehaviour<HUDFPS>
{
    const float MEMORY_DIVIDER = 1048576f;
    [SerializeField]
    bool m_IsDisableVSync = false;
    [Serializable]
    struct FPSValue
    {
        public int m_Value;
        public Color m_Color;
    }
    [HideInInspector]
    FPSValue m_NewValue;
    [HideInInspector]
    FPSValue m_LastValue;
    [HideInInspector]
    FPSValue m_LastAverageValue;
    [HideInInspector]
    FPSValue m_LastMinValue;
    [HideInInspector]
    FPSValue m_LastMaxValue;


    [SerializeField]
    [Range(0.1f, 10f)]
    float m_UpdateIntervalFPS = 0.5f;
    [SerializeField]
    [Range(0, 100)]
    int m_AverageFromSamples = 100;

    int m_CurrentAverageSamples;
    float m_CurrentAverageRaw;
    float[] m_AccumulatedAverageSamples;


    [SerializeField]
    [Range(0.1f, 10f)]
    float m_UpdateIntervalMEM = 0.5f;
    public float m_LastTotalValue = 0;
    public float m_LastAllocatedValue = 0;
    public float m_LastMonoValue = 0;

    [SerializeField]
    float m_LabelHeight = 50.0f;
    float m_LabelWidth = 100.0f;
    public Rect m_StartRect = new Rect(10, 10, 100, 200);

    GUIStyle m_Style;

    bool m_ShowUI = true;

    void Start()
    {
#if !UNITY_EDITOR
        Destroy(this.gameObject);
#endif
        if (m_IsDisableVSync)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }
        m_LastMinValue.m_Value = int.MaxValue;
        m_LastMaxValue.m_Value = int.MinValue;
        StartCoroutine(_UpdateFPSCounter());
        StartCoroutine(_UpdateMemoryCounter());
    }

    void Update()
    {
    }

    IEnumerator _UpdateMemoryCounter()
    {
        while (true)
        {
            m_LastTotalValue = Profiler.GetTotalReservedMemory() / MEMORY_DIVIDER;
            m_LastAllocatedValue = Profiler.GetTotalAllocatedMemory() / MEMORY_DIVIDER;
            m_LastMonoValue = GC.GetTotalMemory(false) / MEMORY_DIVIDER;

            yield return new WaitForSeconds(m_UpdateIntervalMEM);
        }
    }

    private IEnumerator _UpdateFPSCounter()
    {
        while (true)
        {
            float previousUpdateTime = Time.time;
            int previousUpdateFrames = Time.frameCount;

            yield return new WaitForSeconds(m_UpdateIntervalFPS);

            float timeElapsed = Time.time - previousUpdateTime;
            int framesChanged = Time.frameCount - previousUpdateFrames;

            // flooring FPS
            int fps = (int)(framesChanged / (timeElapsed / Time.timeScale));

            m_NewValue.m_Value = fps;
            _UpdateFPSValue(false);
        }
    }

    void _UpdateFPSValue(bool force)
    {
        if (!this.enabled)
        {
            return;
        }
        bool dirty = false;

        if (m_LastValue.m_Value != m_NewValue.m_Value || force)
        {
            m_LastValue.m_Value = m_NewValue.m_Value;
            dirty = true;
        }

        int currentAverageRounded = 0;

        if (0 == m_AverageFromSamples)
        {
            ++m_CurrentAverageSamples;
            m_CurrentAverageRaw += (m_LastValue.m_Value - m_CurrentAverageRaw) / m_CurrentAverageSamples;
        }
        else
        {
            if (null == m_AccumulatedAverageSamples)
            {
                m_AccumulatedAverageSamples = new float[m_AverageFromSamples];
                m_LastAverageValue.m_Value = 0;
                m_CurrentAverageSamples = 0;
                m_CurrentAverageRaw = 0;

                Array.Clear(m_AccumulatedAverageSamples, 0, m_AccumulatedAverageSamples.Length);
            }

            m_AccumulatedAverageSamples[m_CurrentAverageSamples % m_AverageFromSamples] = m_LastValue.m_Value;
            ++m_CurrentAverageSamples;

            float totalFps = 0;

            for (int i = 0; i < m_AverageFromSamples; i++)
            {
                totalFps += m_AccumulatedAverageSamples[i];
            }

            if (m_CurrentAverageSamples < m_AverageFromSamples)
            {
                m_CurrentAverageRaw = totalFps / m_CurrentAverageSamples;
            }
            else
            {
                m_CurrentAverageRaw = totalFps / m_AverageFromSamples;
            }
        }

        currentAverageRounded = Mathf.RoundToInt(m_CurrentAverageRaw);

        if (m_LastAverageValue.m_Value != currentAverageRounded || force)
        {
            m_LastAverageValue.m_Value = currentAverageRounded;
            dirty = true;
        }

        if (dirty)
        {
            if (0 >= m_LastMinValue.m_Value)
            {
                m_LastMinValue.m_Value = int.MaxValue;
            }
            else if (m_LastValue.m_Value < m_LastMinValue.m_Value)
            {
                m_LastMinValue.m_Value = m_LastValue.m_Value;
                dirty = true;
            }

            if (-1 == m_LastMaxValue.m_Value)
            {
                m_LastMaxValue.m_Value = m_LastValue.m_Value;
            }
            else if (m_LastValue.m_Value > m_LastMaxValue.m_Value)
            {
                m_LastMaxValue.m_Value = m_LastValue.m_Value;
                dirty = true;
            }
        }

        if (dirty)
        {
            m_LastValue.m_Color = (m_LastValue.m_Value >= 30) ? Color.green : ((m_LastValue.m_Value > 10) ? Color.red : Color.yellow);
            m_LastAverageValue.m_Color = (m_LastAverageValue.m_Value >= 30) ? Color.green : ((m_LastAverageValue.m_Value > 10) ? Color.red : Color.yellow);
            m_LastMinValue.m_Color = (m_LastMinValue.m_Value >= 30) ? Color.green : ((m_LastMinValue.m_Value > 10) ? Color.red : Color.yellow);
            m_LastMaxValue.m_Color = (m_LastMaxValue.m_Value >= 30) ? Color.green : ((m_LastMaxValue.m_Value > 10) ? Color.red : Color.yellow);
        }
    }

    void OnGUI()
    {
        if (null == m_Style)
        {
            m_Style = new GUIStyle(GUI.skin.label);
            m_Style.normal.textColor = Color.white;
            m_Style.alignment = TextAnchor.MiddleLeft;
        }
        m_ShowUI = GUI.Toggle(new Rect(m_StartRect.xMin + 70, m_StartRect.yMin - 30, 30, 30), m_ShowUI, "");
        if (m_ShowUI)
        {
            m_StartRect = GUI.Window(0, m_StartRect, _OnMyWindow, "");
        }
    }

    void _OnMyWindow(int windowID)
    {
        m_Style.normal.textColor = m_LastValue.m_Color;
        GUI.Label(new Rect(0, 0, m_LabelWidth, m_LabelHeight), "FPS:" + m_LastValue.m_Value, m_Style);
        m_Style.normal.textColor = m_LastAverageValue.m_Color;
        GUI.Label(new Rect(0, m_LabelHeight, m_LabelWidth, m_LabelHeight), "AVE:" + m_LastAverageValue.m_Value, m_Style);
        m_Style.normal.textColor = m_LastMinValue.m_Color;
        GUI.Label(new Rect(0, m_LabelHeight * 2, m_LabelWidth, m_LabelHeight), "MIN:" + m_LastMinValue.m_Value, m_Style);
        m_Style.normal.textColor = m_LastMaxValue.m_Color;
        GUI.Label(new Rect(0, m_LabelHeight * 3, m_LabelWidth, m_LabelHeight), "MAX:" + m_LastMaxValue.m_Value, m_Style);
        m_Style.normal.textColor = Color.white;
        GUI.Label(new Rect(0, m_LabelHeight * 4, m_LabelWidth, m_LabelHeight), "Total:" + m_LastTotalValue, m_Style);
        GUI.Label(new Rect(0, m_LabelHeight * 5, m_LabelWidth, m_LabelHeight), "Alloc:" + m_LastAllocatedValue, m_Style);
        GUI.Label(new Rect(0, m_LabelHeight * 6, m_LabelWidth, m_LabelHeight), "Mono:" + m_LastMonoValue, m_Style);
        if (GUI.Button(new Rect(0, m_LabelHeight * 7, m_LabelWidth, m_LabelHeight), "Refresh:"))
        {
            m_LastAverageValue.m_Value = 0;
            m_CurrentAverageSamples = 0;
            m_CurrentAverageRaw = 0;
            m_LastMinValue.m_Value = int.MaxValue;
            m_LastMaxValue.m_Value = int.MinValue;

            Array.Clear(m_AccumulatedAverageSamples, 0, m_AccumulatedAverageSamples.Length);
        }
        GUI.DragWindow(new Rect(0, 0, Screen.width, Screen.height));
    }
}