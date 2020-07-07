﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniVRM10;
using VrmLib;

namespace UniVRMUtility.VRMViewer
{
    public class FacialExpressionPanel : MonoBehaviour
    {
        [SerializeField]
        private GameObject _facialExpressionPanel;

        [SerializeField]
        private InformationUpdate _informationUpdate;

        [SerializeField]
        private GameObject _predefinedExpression;

        [SerializeField]
        private GameObject _facialExpressionPanelViewportContent;

        [SerializeField]
        private GameObject _sliderCloneExample;

        [SerializeField]
        private GameObject _sliderTextCloneExample;

        [SerializeField]
        private GameObject _facialExpressionBackButton;

        [SerializeField]
        private Transform _sliderTextPanel;

        private GameObject _vrmModel = null;

        // 使える表情数
        private int _validExpNum = 0;
        // 表情の名前
        private string[] _sliderExpName;
        private int[] _correspondingBlendShapeKey;

        // The variable to operate facial expression sliders
        private float _barValue;
        // The place where GameObject and Text name are stored
        private List<GameObject> _objs = new List<GameObject>();
        private List<GameObject> _objsText = new List<GameObject>();
        private bool _lookAtBlendShape;

        public GameObject VrmModel { set { _vrmModel = value; } }
        public bool LookAtBlendShape { set { _lookAtBlendShape = value; } }

        private void LateUpdate()
        {
            ExpressionOperation();
        }

        private void ExpressionOperation()
        {      
            if (_facialExpressionPanel.activeSelf == true)
            {
                if (_vrmModel != null)
                {
                    var proxy = _vrmModel.GetComponent<VRMController>();
                    var blendShapeKeyWeights = _vrmModel.GetComponent<VRMController>().BlendShapeKeyWeights;
                    if (_lookAtBlendShape == true)
                        proxy.LookAtType = VRMController.LookAtTypes.BlendShape;
                    for (int i = 0; i < _validExpNum; i++)
                    {
                        if (_objs[i].GetComponent<UISlider>().IsBeingDragged() == true)
                        {
                            // Operate expression
                            _barValue = _objs[i].GetComponent<Slider>().value;
                            var blendShapeKey = blendShapeKeyWeights.Keys.ElementAt(_correspondingBlendShapeKey[i]);
                            if (blendShapeKey.Preset == BlendShapePreset.LookUp ||
                                blendShapeKey.Preset == BlendShapePreset.LookDown ||
                                blendShapeKey.Preset == BlendShapePreset.LookLeft ||
                                blendShapeKey.Preset == BlendShapePreset.LookRight)
                            {
                                proxy.LookAtType = VRMController.LookAtTypes.Bone;
                                proxy.SetValue(blendShapeKey, _barValue);
                            }
                            else
                                proxy.SetValue(blendShapeKey, _barValue);
                        }
                    }                    
                }
            }
        }

        public void CreateDynamicObject(GameObject vrmModel)
        {
            //************************** Clear expression sliders from previous VRM file
            foreach (var x in _objs)
            {
                GameObject.Destroy(x);
            }
            _objs.Clear();

            foreach (var x in _objsText)
            {
                GameObject.Destroy(x);
            }
            _objsText.Clear();

            _validExpNum = 0;
            _facialExpressionPanelViewportContent.GetComponent<RectTransform>().localPosition = Vector2.zero;
            //******************************

            // Get VRMController
            var controller = vrmModel.GetComponent<VRMController>();

            // Check the number of valid expressions
            foreach (var clip in controller.BlendShapeAvatar.Clips)
            {
                var expressionNums = clip.BlendShapeBindings.Length;
                var expressionMaterialColorNums = clip.MaterialColorBindings.Length;
                var expressionMaterialUVNums = clip.MaterialUVBindings.Length;
                if ((expressionNums > 0) || (expressionMaterialColorNums > 0) || (expressionMaterialUVNums > 0))
                {
                    _validExpNum += 1;
                }
            }

            // Declare the size
            _sliderExpName = new string[_validExpNum];
            _correspondingBlendShapeKey = new int[_validExpNum];
            _validExpNum = 0;

            // Save the valid experssions
            foreach (var (clip, index) in controller.BlendShapeAvatar.Clips.Select((v, i) => (v, i)))
            {
                var expressionNums = clip.BlendShapeBindings.Length;
                var expressionMaterialColorNums = clip.MaterialColorBindings.Length;
                var expressionMaterialUVNums = clip.MaterialUVBindings.Length;
                if ((expressionNums > 0) || (expressionMaterialColorNums > 0) || (expressionMaterialUVNums > 0))
                {
                    var expressionName = clip.BlendShapeName;
                    var dynamicObject = Instantiate(_sliderCloneExample);
                    _objs.Add(dynamicObject);
                    dynamicObject.transform.SetParent(_predefinedExpression.transform);
                    // Scale it back to 1
                    dynamicObject.GetComponent<RectTransform>().localScale = Vector3.one;
                    dynamicObject.name = "Slider_" + expressionName;
                    dynamicObject.SetActive(true);

                    var dynamicObjectText = Instantiate(_sliderTextCloneExample);
                    _objsText.Add(dynamicObjectText);
                    dynamicObjectText.transform.SetParent(_sliderTextPanel);
                    dynamicObjectText.GetComponent<RectTransform>().localScale = Vector3.one;
                    dynamicObjectText.name = "Slider_text_" + expressionName;
                    dynamicObjectText.GetComponent<Text>().text = expressionName;
                    dynamicObjectText.SetActive(true);

                    // Save the name of the valid expression
                    _sliderExpName[_validExpNum] = dynamicObject.name;
                    _correspondingBlendShapeKey[_validExpNum] = index;
                    _validExpNum += 1;
                }
            }

            // Send facial expression data
            _informationUpdate.SetExpression(_objs, _validExpNum);

            // Adjust the Content range and button position in the scroll view of expression
            _facialExpressionPanelViewportContent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 100 + (_validExpNum * 35));
            _facialExpressionBackButton.GetComponent<RectTransform>().localPosition = new Vector3(180, -75 - (_validExpNum * 35), 0);
        }
    }
}