using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("Spline/Spawn On Spline")]
public class SpawnOnSpline : MonoBehaviour, ISerializationCallbackReceiver
{

    public GameObject Prefab;
    public bool FaceDirection = true;
    public float Distance = 1f;
    public SplinePosition Start = 0f;
    public SplinePosition End = 1f;
    public bool LoopDistance = true;
    public Vector2Range Offset = Vector2.zero;

    [SerializeField]
    private Spline spline;
    [SerializeField]
    private Transform container;

    public Spline Spline
    {
        get { return this.spline; }
        set
        {
            this.spline = value;
            this.spline.OnSplineUpdate -= this.QuickRebuild;
            this.spline.OnSplineUpdate += this.QuickRebuild;
            Rebuild();
        }
    }

    public int InstanceCount
    {
        get
        {
            if(this.container == null) return 0;
            return this.container.childCount;
        }
    }

    void Reset()
    {
        ClearAll();
        if (!this.spline) return;
        RegisterSplineEvents();
    }

    void OnEnable()
    {
        if (!this.spline) return;
        RegisterSplineEvents();
    }

    void OnDisable()
    {
        if (!this.spline) return;
        this.spline.OnSplineUpdate -= this.QuickRebuild;
    }

    public void RegisterSplineEvents()
    {
        if (!this.spline) return;
        this.spline.OnSplineUpdate -= this.QuickRebuild;
        this.spline.OnSplineUpdate += this.QuickRebuild;
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        if (!this.spline) return;
        if (!this.Prefab) return;

        ClearAll();
        QuickRebuild();
    }

    public void QuickRebuild()
    {
        //Debug.Log("QuickRebuild");
        if (!this.spline) return;
        if (!this.Prefab) return;

        CreateContainer();

        float startPos = this.Start.GetSplinePosition(this.spline);
        float endPos = this.End.GetSplinePosition(this.spline);
        //Debug.Log("Start: " + startPos + " End: " + endPos);
        if (endPos <= startPos) endPos += 1f;
        float start = this.spline.GetLength(startPos);
        float end = this.spline.GetLength(endPos);
        //Debug.Log("StartLength: " + start + " EndLength: " + end);

        float length = end - start;
        int count = (int)(this.spline.Length / this.Distance);
        float distance = this.LoopDistance ? this.spline.Length / count : this.Distance;
        int instanceCount = this.container.childCount;

        List<int> delete = new List<int>();

        int minDestroy = int.MaxValue;
        int maxAdd = -1;
        int maxMove = -1;

        List<GameObject> destroy = new List<GameObject>();

        //Debug.Log("Count: " + count + " InstanceCount: " + instanceCount);
        for (int i = 0; i < Mathf.Max(count, instanceCount); ++i)
        {
            float d = start + (float)i * distance;
            if (i >= count || d < start || d > end) // Destroy
            {
                if (i < minDestroy) minDestroy = i;
                if(this.container.childCount > i)
                    destroy.Add(this.container.GetChild(i).gameObject);
            }
            else if(i >= instanceCount) // Add
            {
                if (i > maxAdd) maxAdd = i;
                AddPrefab(start + (float)i * distance);
            }
            else // Move
            {
                if (i > maxMove) maxMove = i;
                MovePrefab(i, start + (float)i * distance);
            }
        }

        if (minDestroy == int.MaxValue) minDestroy = 0;
        //Debug.Log("Destroy: " + minDestroy + " Add: " + maxAdd + " Move: " + maxMove);

        foreach(var obj in destroy)
        {
            Destroy(obj);
        }
    }

    private void ClearAll()
    {
        CreateContainer();
        Destroy(this.container.gameObject);
    }

    private void Destroy(GameObject gameObject)
    {
#if UNITY_EDITOR
        DestroyImmediate(gameObject);
#else
        Destroy(gameObject);
#endif
    }

    private GameObject Instantiate(GameObject gameObject)
    {
#if UNITY_EDITOR
        return (GameObject)PrefabUtility.InstantiatePrefab(gameObject);
#else
        return Instantiate(gameObject);
#endif
    }

    private void AddPrefab(float length)
    {
        CreateContainer();
        float t = this.Spline.GetPositionFromLength(length);
        float nt = this.Spline.GetNormalizedPositionFromlength(length);
        var point = Spline.GetLocalOrientedPoint(t);

        var instance = Instantiate(this.Prefab);
        instance.transform.SetParent(this.container, false);

        instance.transform.transform.localPosition = point.LocalToWorld(this.Offset.Evaluate(nt));
        instance.transform.transform.localRotation = this.FaceDirection ? point.Rotation : Quaternion.identity;
    }

    private void MovePrefab(int index, float length)
    {
        float t = this.Spline.GetPositionFromLength(length);
        float nt = this.Spline.GetNormalizedPositionFromlength(length);
        var point = Spline.GetLocalOrientedPoint(t);

        this.container.GetChild(index).localPosition = point.LocalToWorld(this.Offset.Evaluate(nt));
        this.container.GetChild(index).localRotation = this.FaceDirection?point.Rotation:Quaternion.identity;
    }

    private void CreateContainer()
    {
        if (this.container != null) return;

        foreach (Transform trans in this.transform)
        {
            if (trans.GetComponent<SpawnOnSplineContainer>())
            {
                this.container = trans;
                return;
            }
        }

        var obj = new GameObject("Instances");
        obj.transform.SetParent(this.transform,false);
        obj.AddComponent<SpawnOnSplineContainer>();
        obj.hideFlags = HideFlags.HideInHierarchy;
        this.container = obj.transform;
    }

    public void OnBeforeSerialize()
    {
        
    }

    public void OnAfterDeserialize()
    {
        RegisterSplineEvents();
    }
}
