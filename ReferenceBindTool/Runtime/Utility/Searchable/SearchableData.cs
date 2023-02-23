using System;
using UnityEngine;

namespace ReferenceBindTool.Runtime
{
    [Serializable]
    public class SearchableData
    {
        [SerializeField] private int m_Select;
        [SerializeField] private string[] m_Names;

        public SearchableData()
        {
            m_Select = -1;
            m_Names = Array.Empty<string>();
        }
        
        public int Select
        {
            get => m_Select;
            set => m_Select = value;
        }

        public string[] Names
        {
            get => m_Names;
            set => m_Names = value;
        }
    }
}