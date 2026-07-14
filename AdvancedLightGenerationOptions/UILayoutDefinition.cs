using System;
using UnityEngine;
using UnityEngine.Events;

namespace AdvancedLightGenerationOptions
{
    internal sealed class LayoutRow
    {
        public LayoutRow(Action<Transform, Vector2> render)
        {
            Render = render;
        }

        public Action<Transform, Vector2> Render { get; }
    }

    internal sealed class LayoutSection
    {
        public LayoutSection(string id, string header, LayoutRow[] rows)
        {
            Id = id;
            Header = header;
            Rows = rows;
        }

        public string Id { get; }

        public string Header { get; }

        public LayoutRow[] Rows { get; }
    }

    internal sealed class LayoutColumn
    {
        public LayoutColumn(float x, LayoutSection[] sections)
        {
            X = x;
            Sections = sections;
        }

        public float X { get; }

        public LayoutSection[] Sections { get; }
    }

    internal sealed class LayoutButton
    {
        public LayoutButton(string id, string text, UnityAction onClick, string tooltip, bool addColorFade)
        {
            Id = id;
            Text = text;
            OnClick = onClick;
            Tooltip = tooltip;
            AddColorFade = addColorFade;
        }

        public string Id { get; }

        public string Text { get; }

        public UnityAction OnClick { get; }

        public string Tooltip { get; }

        public bool AddColorFade { get; }
    }
}
