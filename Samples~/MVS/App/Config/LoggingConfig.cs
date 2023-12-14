using System;
using System.Collections.Generic;
using System.Linq;
using Extreal.Core.Logging;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.App.Config
{
    [CreateAssetMenu(
        menuName = nameof(MVS) + "/" + nameof(LoggingConfig),
        fileName = nameof(LoggingConfig))]
    public class LoggingConfig : ScriptableObject
    {
        [SerializeField] private List<string> categoryFilters;
        [SerializeField] private List<LogFormat> logFormats;

        [Serializable]
        public class LogFormat
        {
            [SerializeField] private string category;
            [SerializeField] private Color color;

            public string Category => category;
            public Color Color => color;
        }

        public ICollection<string> CategoryFilters => categoryFilters;

        public ICollection<UnityDebugLogFormat> LogFormats =>
            logFormats.Select(logFormat => new UnityDebugLogFormat(logFormat.Category, logFormat.Color)).ToArray();
    }
}
