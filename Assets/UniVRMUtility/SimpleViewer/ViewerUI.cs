﻿using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniHumanoid;
using UnityEngine;
using UnityEngine.UI;
using UniVRM10;

namespace UniVRMUtility.SimpleViewer
{
    public class ViewerUI : MonoBehaviour
    {
        #region UI
        [SerializeField]
        Text m_version;

        [SerializeField]
        Button m_open;

        [SerializeField]
        Toggle m_enableLipSync;

        [SerializeField]
        Toggle m_enableAutoBlink;
        #endregion

        [SerializeField]
        HumanPoseTransfer m_src;

        [SerializeField]
        GameObject m_target;

        [SerializeField]
        GameObject Root;

        [Serializable]
        struct TextFields
        {
            [SerializeField, Header("Info")]
            Text m_textModelTitle;
            [SerializeField]
            Text m_textModelVersion;
            [SerializeField]
            Text m_textModelAuthor;
            [SerializeField]
            Text m_textModelContact;
            [SerializeField]
            Text m_textModelReference;
            [SerializeField]
            RawImage m_thumbnail;

            [SerializeField, Header("CharacterPermission")]
            Text m_textPermissionAllowed;
            [SerializeField]
            Text m_textPermissionViolent;
            [SerializeField]
            Text m_textPermissionSexual;
            [SerializeField]
            Text m_textPermissionCommercial;
            [SerializeField]
            Text m_textPermissionOther;

            [SerializeField, Header("DistributionLicense")]
            Text m_textDistributionLicense;
            [SerializeField]
            Text m_textDistributionOther;

            public void Start()
            {
                m_textModelTitle.text = "";
                m_textModelVersion.text = "";
                m_textModelAuthor.text = "";
                m_textModelContact.text = "";
                m_textModelReference.text = "";

                m_textPermissionAllowed.text = "";
                m_textPermissionViolent.text = "";
                m_textPermissionSexual.text = "";
                m_textPermissionCommercial.text = "";
                m_textPermissionOther.text = "";

                m_textDistributionLicense.text = "";
                m_textDistributionOther.text = "";
            }

            public void UpdateMeta(VrmLib.Model context)
            {
                var meta = context.Vrm.Meta;

                m_textModelTitle.text = meta.Name;
                m_textModelVersion.text = meta.Version;
                m_textModelAuthor.text = meta.Author;
                m_textModelContact.text = meta.ContactInformation;
                m_textModelReference.text = meta.Reference;

                // TODO: 1.0 仕様にする
                m_textPermissionAllowed.text = meta.AvatarPermission.AvatarUsage.ToString();
                m_textPermissionViolent.text = meta.AvatarPermission.IsAllowedViolentUsage.ToString();
                m_textPermissionSexual.text = meta.AvatarPermission.IsAllowedSexualUsage.ToString();
                m_textPermissionCommercial.text = meta.AvatarPermission.CommercialUsage.ToString();
                m_textPermissionOther.text = meta.AvatarPermission.OtherPermissionUrl;

                m_textDistributionLicense.text = meta.RedistributionLicense.ModificationLicense.ToString();
                m_textDistributionOther.text = meta.RedistributionLicense.OtherLicenseUrl;

                if (meta.Thumbnail != null)
                {
                    var thumbnail = new Texture2D(2, 2);
                    thumbnail.LoadImage(meta.Thumbnail.Bytes.ToArray());
                    m_thumbnail.texture = thumbnail;
                }
            }
        }
        [SerializeField]
        TextFields m_texts;

        [Serializable]
        struct UIFields
        {
            [SerializeField]
            Toggle ToggleMotionTPose;

            [SerializeField]
            Toggle ToggleMotionBVH;

            [SerializeField]
            ToggleGroup ToggleMotion;

            Toggle m_activeToggleMotion;

            public void UpdateToggle(Action onBvh, Action onTPose)
            {
                var value = ToggleMotion.ActiveToggles().FirstOrDefault();
                if (value == m_activeToggleMotion)
                    return;

                m_activeToggleMotion = value;
                if (value == ToggleMotionTPose)
                {
                    onTPose();
                }
                else if (value == ToggleMotionBVH)
                {
                    onBvh();
                }
                else
                {
                    Debug.Log("motion: no toggle");
                }
            }
        }
        [SerializeField]
        UIFields m_ui;

        [SerializeField]
        HumanPoseClip m_pose;

        private void Reset()
        {
            var buttons = GameObject.FindObjectsOfType<Button>();
            m_open = buttons.First(x => x.name == "Open");

            var toggles = GameObject.FindObjectsOfType<Toggle>();
            m_enableLipSync = toggles.First(x => x.name == "EnableLipSync");
            m_enableAutoBlink = toggles.First(x => x.name == "EnableAutoBlink");

            var texts = GameObject.FindObjectsOfType<Text>();
            m_version = texts.First(x => x.name == "Version");

            m_src = GameObject.FindObjectOfType<HumanPoseTransfer>();

            m_target = GameObject.FindObjectOfType<TargetMover>().gameObject;
        }

        HumanPoseTransfer m_loaded;

        AIUEO m_lipSync;
        bool m_enableLipSyncValue;
        bool EnableLipSyncValue
        {
            set
            {
                if (m_enableLipSyncValue == value) return;
                m_enableLipSyncValue = value;
                if (m_lipSync != null)
                {
                    m_lipSync.enabled = m_enableLipSyncValue;
                }
            }
        }

        Blinker m_blink;
        bool m_enableBlinkValue;
        bool EnableBlinkValue
        {
            set
            {
                if (m_blink == value) return;
                m_enableBlinkValue = value;
                if (m_blink != null)
                {
                    m_blink.enabled = m_enableBlinkValue;
                }
            }
        }

        async void Start()
        {
            m_version.text = string.Format("SimpleViewer UniVRM-{0}.{1}",
                VRMVersion.MAJOR, VRMVersion.MINOR);
            m_open.onClick.AddListener(OnOpenClicked);

            // load initial bvh
            await LoadMotionAsync(Application.streamingAssetsPath + "/VRM.Samples/Motions/test.txt");

            string[] cmds = System.Environment.GetCommandLineArgs();
            if (cmds.Length > 1)
            {
                StartCoroutine(LoadModelAsync(cmds[1]));
            }

            m_texts.Start();
        }

        async Task LoadMotionAsync(string path)
        {
            var bvh = await Task.Run(() =>
            {
                var context = new UniHumanoid.BvhImporterContext();
                context.Parse(path);
                return context;
            });

            bvh.Load();
            SetMotion(bvh.Root.GetComponent<HumanPoseTransfer>());
        }

        private void Update()
        {
            EnableLipSyncValue = m_enableLipSync.isOn;
            EnableBlinkValue = m_enableAutoBlink.isOn;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (Root != null) Root.SetActive(!Root.activeSelf);
            }

            m_ui.UpdateToggle(EnableBvh, EnableTPose);
        }

        void EnableBvh()
        {
            if (m_loaded != null)
            {
                m_loaded.Source = m_src;
                m_loaded.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer;
            }
        }

        void EnableTPose()
        {
            if (m_loaded != null)
            {
                m_loaded.PoseClip = m_pose;
                m_loaded.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseClip;
            }
        }

        void OnOpenClicked()
        {
#if UNITY_STANDALONE_WIN
            var path = ComDialog.Open("open VRM", "*.vrm", "*.glb", "*.bvh");
#else
            var path = Application.dataPath + "/default.vrm";
#endif
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".gltf":
                case ".glb":
                case ".vrm":
                    StartCoroutine(LoadModelAsync(path));
                    break;

                case ".bvh":
                    LoadMotionAsync(path);
                    break;
            }
        }

        IEnumerator LoadModelAsync(string path)
        {
            Debug.LogFormat("{0}", path);
            var task = Task.Run(() =>
                   {
                       if (!File.Exists(path))
                       {
                           return null;
                       }

                       var vrmModel = VrmLoader.CreateVrmModel(path);
                       return vrmModel;
                   });
            while (!task.IsCompleted)
            {
                // 終了待ち
                yield return null;
            }
            var model = task.Result;
            if (model == null)
            {
                yield break;
            }

            m_texts.UpdateMeta(model);

            // UniVRM-0.XXのコンポーネントを構築する
            var assets = new ModelAsset();

            // build async
            yield return AsyncUnityBuilder.ToUnityAssetAsync(model, assets);

            UniVRM10.ComponentBuilder.Build10(model, assets);

            SetModel(assets);
        }

        void SetModel(ModelAsset asset)
        {
            var go = asset.Root;

            // cleanup
            var loaded = m_loaded;
            m_loaded = null;

            if (loaded != null)
            {
                Debug.LogFormat("destroy {0}", loaded);
                GameObject.Destroy(loaded.gameObject);
            }

            if (go != null)
            {
                var lookAt = go.GetComponent<VRMController>();
                if (lookAt != null)
                {
                    m_loaded = go.AddComponent<HumanPoseTransfer>();
                    m_loaded.Source = m_src;
                    m_loaded.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer;

                    m_lipSync = go.AddComponent<AIUEO>();
                    m_blink = go.AddComponent<Blinker>();

                    lookAt.Gaze = m_target.transform;
                    // lookAt.UpdateType = VRMController.UpdateTypes.LateUpdate; // after HumanPoseTransfer's setPose
                }

                var animation = go.GetComponent<Animation>();
                if (animation && animation.clip != null)
                {
                    animation.Play(animation.clip.name);
                }

                // show mesh
                foreach (var r in asset.Renderers)
                {
                    r.enabled = true;
                }
            }
        }

        void SetMotion(HumanPoseTransfer src)
        {
            m_src = src;
            src.GetComponent<Renderer>().enabled = false;

            EnableBvh();
        }
    }
}
