using System;
using UnityEngine;

namespace LinkShot.Network
{
    /// <summary>
    /// UnityのJsonUtilityはJSON配列をトップレベルで直接パースできないため、
    /// "{"items":...}"に包んでからパースする定番の回避策。PostgRESTのレスポンスは
    /// 配列(例: "[{...}, {...}]")で返ってくるため、GETの結果を扱うときはこれを使う。
    /// </summary>
    public static class JsonArrayUtility
    {
        public static T[] FromJsonArray<T>(string json)
        {
            string wrapped = "{\"items\":" + json + "}";
            var wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper.items ?? Array.Empty<T>();
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }
    }
}
