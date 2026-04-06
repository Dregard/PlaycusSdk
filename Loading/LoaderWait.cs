using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Playcus.Analytics;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Playcus.Loading
{
    /// <summary>
    /// How long Loader need be wait before can continue loading
    /// Documentation https://docs.google.com/document/d/1A6Ce52Xh3iLBiVgYKnjTHNCm4g21k5jln3PdMnZE0To/edit#
    /// </summary>
    public class LoaderWait : MonoBehaviour
    {
        [HelpBox(@"0 - infinity waiting before all previous services load
        >0 - maximum time before loading continue without wait completion", HelpBoxMessageType.Info)]
        [SerializeField] private float timeLimit;
        public float GetTimelimit => timeLimit;
    }
}