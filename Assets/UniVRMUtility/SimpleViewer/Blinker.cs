﻿using System.Collections;
using UnityEngine;
using UniVRM10;
using VrmLib;

namespace UniVRMUtility.SimpleViewer
{
    /// <summary>
    /// 自動瞬きサンプル
    /// </summary>
    public class Blinker : MonoBehaviour
    {
        [SerializeField]
        public VRMController BlendShapes;
        private void Reset()
        {
            BlendShapes = GetComponent<VRMController>();
        }

        [SerializeField]
        float m_interVal = 5.0f;

        [SerializeField]
        float m_closingTime = 0.06f;

        [SerializeField]
        float m_openingSeconds = 0.03f;

        [SerializeField]
        float m_closeSeconds = 0.1f;

        protected Coroutine m_coroutine;

        //static readonly string BLINK_NAME = BlendShapePreset.Blink.ToString();

        float m_nextRequest;
        bool m_request;
        public bool Request
        {
            get { return m_request; }
            set
            {
                if (Time.time < m_nextRequest)
                {
                    return;
                }
                m_request = value;
                m_nextRequest = Time.time + 1.0f;
            }
        }

        protected IEnumerator BlinkRoutine()
        {
            while (true)
            {
                var waitTime = Time.time + Random.value * m_interVal;
                while (waitTime > Time.time)
                {
                    if (Request)
                    {
                        m_request = false;
                        break;
                    }
                    yield return null;
                }

                // close
                var value = 0.0f;
                var closeSpeed = 1.0f / m_closeSeconds;
                while (true)
                {
                    value += Time.deltaTime * closeSpeed;
                    if (value >= 1.0f)
                    {
                        break;
                    }

                    BlendShapes.SetPresetValue(BlendShapePreset.Blink, value);
                    yield return null;
                }
                BlendShapes.SetPresetValue(BlendShapePreset.Blink, 1.0f);

                // wait...
                yield return new WaitForSeconds(m_closingTime);

                // open
                value = 1.0f;
                var openSpeed = 1.0f / m_openingSeconds;
                while (true)
                {
                    value -= Time.deltaTime * openSpeed;
                    if (value < 0)
                    {
                        break;
                    }

                    BlendShapes.SetPresetValue(BlendShapePreset.Blink, value);
                    yield return null;
                }
                BlendShapes.SetPresetValue(BlendShapePreset.Blink, 0);
            }
        }

        private void Awake()
        {
            if (BlendShapes == null) BlendShapes = GetComponent<VRMController>();
        }

        private void OnEnable()
        {
            m_coroutine = StartCoroutine(BlinkRoutine());
        }

        private void OnDisable()
        {
            if (m_coroutine != null)
            {
                StopCoroutine(m_coroutine);
                m_coroutine = null;
            }
        }
    }
}
