using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public static class Extensions
{
    public static void SetChildrenActive(this Transform self, bool newState, bool applyStateToAllChildren = true)
    {
        for (int child = 0; child < self.childCount; ++child)
        {
            var obj = self.GetChild(child).gameObject;
            obj.SetActive(newState);

            if (!applyStateToAllChildren)
                continue;

            SetChildrenActive(obj.transform, newState, applyStateToAllChildren);
        }
    }

    public static void SetActive(this Transform self, bool newState)
    {
        self.gameObject.SetActive(newState);
    }

    public static CinemachineVirtualCamera GetLiveCamera(this Camera cam)
    {
        return cam.GetComponent<CinemachineBrain>().ActiveVirtualCamera as CinemachineVirtualCamera;
    }

    public static Sequence DOTransform(this Transform owner, Vector3 position, Vector3 rotationEuler, Vector3 scale, float duration)
    {
        var transform = DOTween.Sequence();
        transform.Insert(0, owner.DOMove(position, duration));
        transform.Insert(0, owner.DORotate(rotationEuler, duration));
        transform.Insert(0, owner.DOScale(scale, duration));
        return transform;

    }


    public static Sequence DORectTransform(this RectTransform owner, Vector2 position, Vector3 rotationEuler, Vector3 scale, float duration)
    {
        var transform = DOTween.Sequence();
        transform.Insert(0, owner.DOAnchorPos(position, duration));
        transform.Insert(0, owner.DORotate(rotationEuler, duration));
        transform.Insert(0, owner.DOSizeDelta(scale, duration));
        return transform;

    }
}



namespace ExtraUtilities
{
    public class RandomSet<T>
    {
        protected List<T> set;
        public RandomSet() { set = new List<T>(); }
        public RandomSet(IEnumerable<T> source)
        {
            set = new List<T>();
            set.AddRange(source);
        }
        public void AddRange(IEnumerable<T> newSource)
        {
            set.AddRange(newSource);
        }
        public void Add(T value)
        {
            set.Add(value);
        }
        public T RandomElement => Get();
        protected virtual T Get()
        {
            return set[Random.Range(0, set.Count)];
        }
    }
    public class WeighedRandomSet<T> : RandomSet<T>
    {
        private List<float> weights;
        public WeighedRandomSet() { weights = new List<float>(); }
        public WeighedRandomSet(Dictionary<T, float> source)
        {
            set = new List<T>();
            weights = new List<float>();

            foreach (var (value, weight) in source)
            {
                set.Add(value);
                weights.Add(weight);
            }
        }
        public void Add(T value, float weight)
        {
            base.Add(value);
            weights.Add(weight);
        }

        public void AddRange(Dictionary<T, float> source)
        {
            foreach (var (value, weight) in source)
            {
                set.Add(value);
                weights.Add(weight);
            }
        }
        protected override T Get()
        {
            var totalWeight = 0.0f;
            foreach (var weight in weights)
                totalWeight += weight;

            var randomVal = Random.Range(0, totalWeight);
            for (int i = 0; i < set.Count; i++)
            {
                randomVal -= weights[i];
                if (randomVal <= 0)
                {
                    return set[i];
                }
            }

            return default;
        }
    }
}

