using System;
using System.Collections.Generic;
using CodeBind;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LoopScrollRect))]
    [CodeBindName("CommonLoopScrollRect")]
    public sealed class CommonLoopScrollRect : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        [SerializeField]
        [OnValueChanged("OnItemTemplateChanged")]
        private GameObject m_ItemTemplate;

        private int m_NumItems;
        private Stack<Transform> m_ItemPool = new Stack<Transform>();
        [SerializeField]
        private LoopScrollRect m_LoopScrollRect;

        [IgnorePropertyDeclaration]
        public Action<int, Transform> itemRenderer { set; private get; }

        [ShowInInspector]
        [DisableInEditorMode]
        [IgnorePropertyDeclaration]
        public int numItems
        {
            set
            {
                m_NumItems = value;
                m_LoopScrollRect.totalCount = m_NumItems;
                Refresh();
            }
            get => m_NumItems;
        }

        public void Refresh()
        {
            m_LoopScrollRect.RefillCells();
        }

        public GameObject GetObject(int index)
        {
            if (m_ItemPool.Count == 0)
            {
                return Instantiate(m_ItemTemplate);
            }
            Transform candidate = m_ItemPool.Pop();
            candidate.gameObject.SetActive(true);
            return candidate.gameObject;
        }

        public void ReturnObject(Transform trans)
        {
            //trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
            trans.gameObject.SetActive(false);
            trans.SetParent(transform, false);
            m_ItemPool.Push(trans);
        }

        public void ProvideData(Transform trans, int idx)
        {
            //trans.SendMessage("ScrollCellIndex", idx);
            if (itemRenderer != null)
            {
                itemRenderer.Invoke(idx, trans);
            }
        }

        private void Awake()
        {
            if (m_LoopScrollRect == null)
            {
                m_LoopScrollRect = GetComponent<LoopScrollRect>();
            }

            if (m_LoopScrollRect == null)
            {
                Log.Error($"LoopScrollRect is missing on '{this.name}'.");
                return;
            }

            if (m_ItemTemplate == null && m_LoopScrollRect.content != null && m_LoopScrollRect.content.childCount > 0)
            {
                m_ItemTemplate = m_LoopScrollRect.content.GetChild(0).gameObject;
            }

            if (m_ItemTemplate == null)
            {
                Log.Error($"Item template is missing on '{this.name}'.");
                return;
            }

            m_LoopScrollRect.prefabSource = this;
            m_LoopScrollRect.dataSource = this;
            m_ItemPool.Push(m_ItemTemplate.transform);
            m_ItemTemplate.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_LoopScrollRect = GetComponent<LoopScrollRect>();
        }

        [IgnoreLogMethod]
        private void OnItemTemplateChanged()
        {
            if (m_ItemTemplate != null && m_ItemTemplate.transform.parent != m_LoopScrollRect.content)
            {
                Debug.LogError($"Item template must be a child of LoopScrollRect '{this.name}' content.");
                m_ItemTemplate = null;
            }
        }
#endif
    }
}
