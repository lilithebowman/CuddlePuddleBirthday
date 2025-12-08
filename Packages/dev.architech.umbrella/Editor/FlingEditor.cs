using ArchiTech.SDK;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.Umbrella.Editor
{
    [CustomEditor(typeof(Fling))]
    public class FlingEditor : SDK.Editor.ATBehaviourEditor
    {
        private const int RESOLUTION = 60;
        private const float NEAR_ZERO = 0.00000005f;
        private Fling script;
        private float totalTravelTime;
        private float totalTravelDistance;
        private Transform[] targetObject;
        private Vector3[] targetOffset;
        private Vector3[] targetPosition;
        private Vector3[] startAnchor;
        private Vector3[] endAnchor;
        public FlingInterpolationMode[] interpolationMode;
        private float[] segmentTravelTime;
        private float[] segmentLength;
        private bool[] seamless;
        private AudioSource[] sfx;

        private Vector2 scrollPos;
        private bool repaint;
        private bool addIndex = false;
        private int removeIndex = -1;
        private bool normalizeArrays = true;
        private Vector3 lastLocalObjPos;
        private Tool lastTool = Tool.Move;


        protected override bool autoRenderVariables => false;

        protected override void LoadData()
        {
            script = (Fling)target;
            targetObject = script.targetObject;
            targetOffset = script.targetOffset;
            targetPosition = script.targetPosition;
            startAnchor = script.startAnchor;
            endAnchor = script.endAnchor;
            interpolationMode = script.interpolationMode;
            segmentTravelTime = script.segmentTravelTime;
            segmentLength = script.segmentLength;
            seamless = script.seamless;
            sfx = script.sfx;
            totalTravelTime = script.totalTravelTime;
            totalTravelDistance = script.totalTravelDistance;
            lastLocalObjPos = script._EDITOR_lastLocalObjPos;
            if (normalizeArrays || targetPosition == null)
                normalizeArrayData();
            normalizeArrays = false;
        }

        protected override void SaveData()
        {
            if (addIndex) addTarget();
            else if (removeIndex > -1) removeTarget(removeIndex);
            addIndex = false;
            removeIndex = -1;

            script.targetObject = CopyArray(targetObject);
            script.targetOffset = CopyArray(targetOffset);
            script.targetPosition = CopyArray(targetPosition);
            script.startAnchor = CopyArray(startAnchor);
            script.endAnchor = CopyArray(endAnchor);
            script.interpolationMode = interpolationMode;
            script.segmentTravelTime = segmentTravelTime;
            script.segmentLength = segmentLength;
            script.seamless = seamless;
            script.sfx = sfx;
            script.totalTravelTime = totalTravelTime;
            script.totalTravelDistance = totalTravelDistance;
            script._EDITOR_lastLocalObjPos = lastLocalObjPos;
            if (repaint) SceneView.RepaintAll();
            repaint = false;
        }

        private void normalizeArrayData()
        {
            targetPosition = NormalizeArray(targetPosition, 0, typeof(Vector3));
            int len = targetPosition.Length;
            targetObject = NormalizeArray(targetObject, len, typeof(Transform));
            targetOffset = NormalizeArray(targetOffset, len, typeof(Vector3));
            startAnchor = NormalizeArray(startAnchor, len, typeof(Vector3));
            endAnchor = NormalizeArray(endAnchor, len, typeof(Vector3));
            interpolationMode = NormalizeArray(interpolationMode, len, typeof(FlingInterpolationMode));
            segmentTravelTime = NormalizeArray(segmentTravelTime, len, typeof(float));
            segmentLength = NormalizeArray(segmentLength, len, typeof(float));
            seamless = NormalizeArray(seamless, len, typeof(bool));
            sfx = NormalizeArray(sfx, len, typeof(AudioSource));
        }

        private void addTarget()
        {
            Debug.Log($"Adding target {targetPosition.Length + 1}");
            int newIndex = targetPosition.Length;
            targetObject = AddArrayItem(targetObject);
            targetOffset = AddArrayItem(targetOffset);
            targetPosition = AddArrayItem(targetPosition);
            startAnchor = AddArrayItem(startAnchor);
            endAnchor = AddArrayItem(endAnchor);
            interpolationMode = AddArrayItem(interpolationMode);
            seamless = AddArrayItem(seamless);
            segmentTravelTime = AddArrayItem(segmentTravelTime);
            segmentLength = AddArrayItem(segmentLength);
            sfx = AddArrayItem(sfx);

            // Fancy logic for making the new anchors spawn in reasonable and easily accessed positions
            Vector3 startTarget = script.transform.position;
            if (newIndex > 0)
            {
                Transform targetObj = targetObject[newIndex - 1];
                startTarget = targetObj != null ? targetObj.position : targetPosition[newIndex - 1];
            }

            Vector3 endTarget = startTarget + Vector3.one * 2;

            targetPosition[newIndex] = endTarget;
            startAnchor[newIndex] = new Vector3(1f, 1f, 0) + startTarget;
            endAnchor[newIndex] = new Vector3(-1f, -1f, 0f) + endTarget;
            segmentTravelTime[newIndex] = 1;
            repaint = true;
        }

        private void removeTarget(int index)
        {
            Debug.Log($"Removing target {index + 1}");
            targetObject = RemoveArrayItem(targetObject, index);
            targetOffset = RemoveArrayItem(targetOffset, index);
            targetPosition = RemoveArrayItem(targetPosition, index);
            startAnchor = RemoveArrayItem(startAnchor, index);
            endAnchor = RemoveArrayItem(endAnchor, index);
            interpolationMode = RemoveArrayItem(interpolationMode, index);
            seamless = RemoveArrayItem(seamless, index);
            segmentTravelTime = RemoveArrayItem(segmentTravelTime, index);
            segmentLength = RemoveArrayItem(segmentLength, index);
            sfx = RemoveArrayItem(sfx, index);
            repaint = true;
        }

        protected override void RenderChangeCheck()
        {
            renderCurrentState();
            calculateNextState();
        }

        private void renderCurrentState()
        {
            bool useFlingObject = script.flingObject != null;
            if (DrawVariablesByName(nameof(Fling._EDITOR_editAnchors)))
            {
                if (script._EDITOR_editAnchors)
                {
                    lastTool = Tools.current;
                    Tools.current = Tool.None;
                }
                else Tools.current = lastTool;

                repaint = true;
            }

            if (DrawVariablesByName(nameof(Fling.relativeAnchors))) repaint = true;
            DrawVariablesByName(nameof(Fling.smoothEntry));
            DrawVariablesByName(nameof(Fling.flingObject));
            if (useFlingObject) DrawVariablesByName(nameof(Fling.useObjectOffset));
            else
            {
                DrawVariablesByName(nameof(Fling.movementMode));
                DrawVariablesByName(nameof(Fling.allowJumpExit));
                DrawVariablesByName(nameof(Fling.retainVelocityOnExit));
            }

            DrawVariablesByName(nameof(Fling.pathEndMode));
            DrawVariablesByName(nameof(Fling.timeScale));
            EditorGUILayout.LabelField($"{I18n.Tr("Total Travel Distance")}: {script.totalTravelDistance}");
            EditorGUILayout.LabelField($"{I18n.Tr("Current Segment")}: {script.currentSegment}");
            EditorGUILayout.LabelField($"{I18n.Tr("Normalized Time")}: {script.normalizedTime}");
            DrawVariablesByName(nameof(Fling.totalTravelTime));
            if (script.totalTravelTime < 0) script.totalTravelTime = 0; // keep non-negative

            // editAnchors = EditorGUILayout.Toggle("Edit Anchors", editAnchors);
            // if (editAnchors != script._EDITOR_editAnchors) repaint = true;
            // relativeAnchors = EditorGUILayout.Toggle("Relative Anchors", relativeAnchors);
            // if (relativeAnchors != script.relativeAnchors) repaint = true;
            // smoothEntry = EditorGUILayout.Toggle("Interpolate Start Position", smoothEntry);
            // flingObject = (Transform)EditorGUILayout.ObjectField("Fling Object Instead", flingObject, typeof(Transform), true);
            // if (useFlingObject)
            //     useObjectOffset = EditorGUILayout.Toggle("Start From Relative Offset", useObjectOffset);
            // else
            // {
            //     movementMode = (FlingPlayerMoveMode)EditorGUILayout.EnumPopup("Player Movement Style", movementMode);
            //     allowJumpExit = EditorGUILayout.Toggle("Allow Jump to Exit", allowJumpExit);
            //     retainVelocityOnExit = EditorGUILayout.Toggle("Retain Velocity on Exit", retainVelocityOnExit);
            // }
            // pathEndMode = (FlingPathEndMode)EditorGUILayout.EnumPopup("End of Path Mode", pathEndMode);
            // timeScale = EditorGUILayout.FloatField("Time Scale", timeScale);
            // EditorGUILayout.LabelField($"Total Travel Distance: {totalTravelDistance}");
            // EditorGUILayout.LabelField($"Current Segment: {script.currentSegment}");
            // EditorGUILayout.LabelField($"Normalized Time: {script.normalizedTime}");
            // totalTravelTime = EditorGUILayout.FloatField("Total Travel Time (seconds)", totalTravelTime);
            // if (totalTravelTime < 0) totalTravelTime = 0; // keep non-negative

            // SEGMENT CONTROLS

            if (GUILayout.Button("+")) addIndex = true;
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(0f), GUILayout.ExpandHeight(true), GUILayout.MaxHeight(400f));
            if (targetPosition != null)
            {
                EditorGUIUtility.labelWidth = 40;
                Vector3 from = script.transform.position;
                Vector3 positionDiff = from - lastLocalObjPos;
                lastLocalObjPos = from;

                for (int i = 0; i < targetPosition.Length; i++)
                {
                    Transform targetObj = targetObject[i];
                    Vector3 targetShift = targetOffset[i];
                    Vector3 targetPos = targetPosition[i];
                    Vector3 inAnchor = startAnchor[i];
                    Vector3 outAnchor = endAnchor[i];
                    FlingInterpolationMode mode = interpolationMode[i];
                    AudioSource sound = sfx[i];
                    float travelTime = segmentTravelTime[i];
                    float length = segmentLength[i];
                    bool noSeam = seamless[i];

                    bool useTargetObj = targetObj != null;
                    bool disableSeamless = i == 0 || mode < FlingInterpolationMode.PARABOLIC;
                    if (i > 0)
                    {
                        var prevTarget = targetObject[i - 1];
                        if (prevTarget != null) from = prevTarget.position + targetOffset[i - 1];
                        else from = targetPosition[i - 1];
                    }

                    Vector3 to = useTargetObj ? targetObj.position + targetShift : targetPos;
                    Vector3 midpoint = Vector3.Lerp(from, to, 0.5f);
                    // in anchor for segment 0 moves based on the local script transform position
                    if (i == 0 && script.relativeAnchors) inAnchor += positionDiff;

                    using (HBox)
                    {
                        using (VArea)
                        {
                            // SEGMENT TARGET

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(I18n.Tr("Target") + $" {i + 1}", GUILayout.Width(65f));
                            Vector3 newPositionDiff = Vector3.zero;
                            if (!useTargetObj)
                            {
                                // if no target is set, inline the object field with the header, and display the position Vector
                                EditorGUILayout.Space(10f);
                                targetObj = (Transform)EditorGUILayout.ObjectField(GUIContent.none, targetObj, typeof(Transform), true, GUILayout.MinWidth(120f));
                                if (targetObj != targetObject[i]) repaint = true;
                                EditorGUILayout.EndHorizontal();

                                targetPos = EditorGUILayout.Vector3Field(GUIContent.none, targetPos, GUILayout.MinWidth(180f));
                                if (targetPos != targetPosition[i]) repaint = true;
                                // track target positional change since last check
                                newPositionDiff = targetPos - targetPosition[i];
                            }
                            else
                            {
                                // if target exists, hide the position vector and show the object field in place of it.
                                targetObj = (Transform)EditorGUILayout.ObjectField(GUIContent.none, targetObj, typeof(Transform), true, GUILayout.MinWidth(120f));
                                if (targetObj != targetObject[i]) repaint = true;
                                if (targetObj != null)
                                {
                                    var actualPos = targetObj.position + targetShift;
                                    // track the object's positional change from last check
                                    newPositionDiff = actualPos - targetPos;
                                    // update target position to object position
                                    targetPos = actualPos;
                                }

                                EditorGUILayout.EndHorizontal();

                                targetShift = EditorGUILayout.Vector3Field(GUIContent.none, targetShift, GUILayout.MinWidth(180f));
                                if (targetShift != targetOffset[i]) repaint = true;
                            }


                            // HANDLE RELATIVE ANCHORS

                            // convert the anchors to be relative if flag is set
                            if (script.relativeAnchors)
                            {
                                // update in anchor from the previous segment's target diff
                                if (i > 0) inAnchor += positionDiff;
                                // update target diff to current segment data
                                positionDiff = newPositionDiff;
                                // update out anchor from the current segment's target diff
                                outAnchor += positionDiff;

                                // shift anchors into relative space for displaying the values in the inspector
                                inAnchor -= from;
                                midpoint -= from;
                                outAnchor -= to;
                                // fix near zero values to be actually zero instead of forcing scientific notation.
                                inAnchor = new Vector3(
                                    Mathf.Abs(inAnchor.x) <= NEAR_ZERO ? 0 : inAnchor.x,
                                    Mathf.Abs(inAnchor.y) <= NEAR_ZERO ? 0 : inAnchor.y,
                                    Mathf.Abs(inAnchor.z) <= NEAR_ZERO ? 0 : inAnchor.z
                                );
                                outAnchor = new Vector3(
                                    Mathf.Abs(outAnchor.x) <= NEAR_ZERO ? 0 : outAnchor.x,
                                    Mathf.Abs(outAnchor.y) <= NEAR_ZERO ? 0 : outAnchor.y,
                                    Mathf.Abs(outAnchor.z) <= NEAR_ZERO ? 0 : outAnchor.z
                                );
                            }

                            // IN ANCHOR
                            if (mode == FlingInterpolationMode.BOUNCE)
                            {
                                EditorGUILayout.LabelField(I18n.Tr("Bounce Height"), GUILayout.Width(100f));
                                float height = inAnchor.y - midpoint.y;
                                height = EditorGUILayout.FloatField(GUIContent.none, height, GUILayout.Width(180f));
                                inAnchor.y = midpoint.y + height;
                            }
                            else
                            {
                                EditorGUILayout.LabelField(I18n.Tr("In Anchor"), GUILayout.Width(100f));
                                if (mode < FlingInterpolationMode.BOUNCE)
                                {
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.Vector3Field(GUIContent.none, Vector3.zero, GUILayout.MinWidth(180f));
                                    EditorGUI.EndDisabledGroup();
                                }
                                else if (i > 0 && noSeam)
                                {
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.Vector3Field(GUIContent.none, inAnchor, GUILayout.MinWidth(180f));
                                    EditorGUI.EndDisabledGroup();
                                }
                                else inAnchor = EditorGUILayout.Vector3Field(GUIContent.none, inAnchor, GUILayout.MinWidth(180f));
                            }

                            if (!inAnchor.Equals(startAnchor[i] - from)) repaint = true;

                            // OUT ANCHOR

                            EditorGUILayout.LabelField(I18n.Tr("Out Anchor"), GUILayout.Width(100f));
                            bool outMatchIn = false;
                            if (mode < FlingInterpolationMode.CUBIC)
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.Vector3Field(GUIContent.none, Vector3.zero, GUILayout.MinWidth(180f));
                                EditorGUI.EndDisabledGroup();
                                if (mode > FlingInterpolationMode.LINEAR)
                                {
                                    // if outAnchor is disabled and inAnchor is enabled, move it to inAnchor with a bit of offset
                                    outAnchor = inAnchor;
                                    if (!noSeam)
                                    {
                                        Quaternion facing = Quaternion.identity;
                                        Vector3 dir = new Vector3(to.x, 0, to.z) - new Vector3(from.x, 0, from.z);
                                        facing.SetLookRotation(dir);
                                        outAnchor += facing * Vector3.forward;
                                    }

                                    outMatchIn = true;
                                }
                            }
                            else outAnchor = EditorGUILayout.Vector3Field(GUIContent.none, outAnchor, GUILayout.MinWidth(180f));

                            if (!outAnchor.Equals(endAnchor[i] - to)) repaint = true;

                            // convert anchors back to worldspace if flag is set
                            if (script.relativeAnchors)
                            {
                                inAnchor += from;
                                outAnchor += outMatchIn ? from : to;
                            }

                            targetObject[i] = targetObj;
                            targetOffset[i] = targetShift;
                            targetPosition[i] = targetPos;
                            startAnchor[i] = inAnchor;
                            endAnchor[i] = outAnchor;
                        }

                        using (VArea)
                        {
                            EditorGUILayout.LabelField(I18n.Tr("Interpolation Mode"), GUILayout.Width(120f));
                            mode = (FlingInterpolationMode)EditorGUILayout.EnumPopup(mode);
                            if (mode != interpolationMode[i]) repaint = true;

                            EditorGUIUtility.labelWidth = 75;
                            EditorGUI.BeginDisabledGroup(disableSeamless);
                            if (disableSeamless) EditorGUILayout.Toggle(I18n.Tr("Seamless"), false);
                            else noSeam = EditorGUILayout.Toggle(I18n.Tr("Seamless"), noSeam);
                            EditorGUI.EndDisabledGroup();
                            if (noSeam != seamless[i]) repaint = true;

                            EditorGUILayout.LabelField(I18n.Tr("Travel Distance") + $" {length}");

                            travelTime = EditorGUILayout.FloatField(I18n.Tr("Travel Time"), travelTime);
                            if (travelTime < 0) travelTime = 0;

                            EditorGUIUtility.labelWidth = 40;
                            sound = (AudioSource)EditorGUILayout.ObjectField(I18n.Tr("SFX"), sound, typeof(AudioSource), true);

                            interpolationMode[i] = mode;
                            sfx[i] = sound;
                            seamless[i] = noSeam;
                            segmentTravelTime[i] = travelTime;
                        }

                        EditorGUILayout.Space(5f, false);
                        if (GUILayout.Button(I18n.Tr("Remove"))) removeIndex = i;
                    }
                }

                EditorGUIUtility.labelWidth = 0; // resets value to default
            }

            EditorGUILayout.EndScrollView();
        }

        private void calculateNextState()
        {
            // default the start of the arc to the game object's position just as a reference point
            Vector3 from = script.transform.position;
            float distance = 0f;
            float time = 0f;
            for (int i = 0; i < targetPosition.Length; i++)
            {
                FlingInterpolationMode mode = interpolationMode[i];
                Transform targetObj = targetObject[i];
                Vector3 targetPos = targetPosition[i];
                Vector3 inAnchor = startAnchor[i];
                Vector3 outAnchor = endAnchor[i];
                float length = segmentLength[i];
                bool noSeam = seamless[i] && mode > FlingInterpolationMode.BOUNCE;

                // bool useTargetObject = targetObj != null;

                // Seamless mode calculations
                // when seamless, the in tangent should be the exact opposite of the out tangent from the previous segment
                if (i > 0 && noSeam)
                {
                    // focus point is always the target of the previous segment
                    Vector3 focusPoint = targetPosition[i - 1];
                    // determine which point is the control based on the mode of the previous segment
                    Vector3 controlPoint = getSeamlessAnchorForSegment(i);
                    // move the start anchor to the exact opposite side of the focus point from the control point
                    startAnchor[i] = Vector3.LerpUnclamped(focusPoint, controlPoint, -1f);
                }


                // Calculates the path lengths

                Vector3 to = targetPos;
                Vector3 lastPoint = from;
                Vector3 nextPoint = lastPoint;
                Vector3 midPoint = Vector3.Lerp(from, to, 0.5f);
                float weight = 1f; // temp placeholder. Looking to add a weight calculation to the algebra, but it doesn't do anything yet
                float height = inAnchor.y - midPoint.y;
                length = 0f; // reset current segment length

                for (int j = 1; j <= RESOLUTION; j++)
                {
                    var t = j / (float)RESOLUTION;
                    switch (mode)
                    {
                        case FlingInterpolationMode.LINEAR:
                            nextPoint = Vector3.Lerp(from, to, t);
                            break;
                        case FlingInterpolationMode.BOUNCE:
                            nextPoint = Algebra.GetParabolicPoint(from, to, height, t);
                            break;
                        case FlingInterpolationMode.PARABOLIC:
                            nextPoint = Algebra.GetQuadraticBezierPoint(from, inAnchor, to, t, weight);
                            break;
                        case FlingInterpolationMode.CUBIC:
                            nextPoint = Algebra.GetCubicBezierPoint(from, inAnchor, outAnchor, to, t, weight);
                            break;
                        // case FlingInterpolationMode.HERMITE:
                        //     nextPoint = Algebra.GetHermiteCurvePoint(from, inAnchor, outAnchor, to, t);
                        //     break;
                    }

                    length += Vector3.Distance(lastPoint, nextPoint);
                    lastPoint = nextPoint;
                }

                segmentLength[i] = length;
                distance += length;
                time += segmentTravelTime[i];
                from = to;
            }

            totalTravelDistance = distance;

            if (Mathf.Abs(totalTravelTime - script.totalTravelTime) >= 0.0001f)
            {
                // Recalculate all travel times if the total time was changed in the global field
                for (int i = 0; i < targetPosition.Length; i++)
                {
                    if (totalTravelTime == 0) segmentTravelTime[i] = 0;
                    else segmentTravelTime[i] = segmentLength[i] / totalTravelDistance * totalTravelTime;
                }
            }
            else totalTravelTime = time;
        }

        void OnSceneGUI()
        {
            LoadData();
            if (script == null || script.targetPosition == null || script.targetPosition.Length == 0) return; // skip when targets are empty
            // default the start of the arc to the game object's position just as a reference point
            Vector3 from = script.transform.position;
            // float travelDistance = 0f;
            EditorGUI.BeginChangeCheck();
            Vector3 positionDiff = from - lastLocalObjPos;
            lastLocalObjPos = from;
            // conditional to enforce updating anchor values when the root transform moves
            if (!positionDiff.Equals(Vector3.zero)) ForceSave();
            for (int i = 0; i < targetPosition.Length; i++)
            {
                Transform targetObj = targetObject[i];
                Vector3 targetShift = targetOffset[i];
                Vector3 to = targetPosition[i];
                FlingInterpolationMode mode = interpolationMode[i];
                Vector3 inAnchor = startAnchor[i];
                Vector3 outAnchor = endAnchor[i];


                Vector3 midPoint = Vector3.Lerp(from, to, 0.5f);
                // float height = inAnchor.y - midPoint.y;
                // Vector3 lastPoint = from;
                // Vector3 nextPoint = lastPoint;
                Vector3 newPositionDiff = Vector3.zero;

                // in anchor for segment 0 moves based on the local script transform position
                if (i == 0 && script.relativeAnchors) inAnchor += positionDiff;

                // if the mode is bounce, offset the anchor to be aligned on the calculated arc


                Vector3 towards = new Vector3(to.x, 0, to.z) - new Vector3(from.x, 0, from.z);
                Quaternion facing = Quaternion.identity;
                bool isSeamless = i > 0 && seamless[i] && mode > FlingInterpolationMode.BOUNCE;
                // draw the handles for manipulating the anchors and position targets
                if (targetObj != null)
                {
                    var pos = targetObj.position;
                    // track the object's positional change from last check
                    if (script.relativeAnchors) newPositionDiff = pos + targetShift - to;
                    // get the updated target offset value
                    if (script._EDITOR_editAnchors) targetShift = Handles.PositionHandle(targetShift + pos, facing) - pos;
                    // update target position to object position
                    to = pos + targetShift;
                }
                else
                {
                    Vector3 newPos = to;
                    if (script._EDITOR_editAnchors) newPos = Handles.PositionHandle(to, facing);
                    // track the target's positional change
                    if (script.relativeAnchors) newPositionDiff = newPos - to;
                    // update the target position
                    to = newPos;
                }

                if (script.relativeAnchors)
                {
                    // update anchors based on the relative movement of their respective targets
                    if (i > 0) inAnchor += positionDiff;
                    positionDiff = newPositionDiff;
                    outAnchor += positionDiff;
                    // conditional to enforce updating anchor values when the target position moves
                    if (!positionDiff.Equals(Vector3.zero)) ForceSave();
                }

                if (!Vector3.zero.Equals(towards)) facing.SetLookRotation(towards);

                Vector3 seamlessTarget = getSeamlessAnchorForSegment(i);

                switch (mode)
                {
                    case FlingInterpolationMode.LINEAR: break;
                    case FlingInterpolationMode.BOUNCE:
                        Handles.color = Color.green;
                        if (!isSeamless && script._EDITOR_editAnchors) inAnchor = Handles.Slider(inAnchor, Vector3.up);
                        Handles.color = Color.yellow;
                        break;
                    case FlingInterpolationMode.PARABOLIC:
                        if (isSeamless)
                            inAnchor = Vector3.LerpUnclamped(from, seamlessTarget, -1f);
                        else if (script._EDITOR_editAnchors) inAnchor = Handles.PositionHandle(inAnchor, facing);
                        break;
                    default: // when using all 4 points
                        if (isSeamless)
                            inAnchor = Vector3.LerpUnclamped(from, seamlessTarget, -1f);
                        else if (script._EDITOR_editAnchors) inAnchor = Handles.PositionHandle(inAnchor, facing);
                        if (script._EDITOR_editAnchors) outAnchor = Handles.PositionHandle(outAnchor, facing);
                        break;
                }

                // draws dotted lines representing the tangent point connection
                switch (mode)
                {
                    case FlingInterpolationMode.LINEAR: break;
                    case FlingInterpolationMode.BOUNCE:
                        inAnchor = new Vector3(midPoint.x, inAnchor.y, midPoint.z);
                        break;
                    case FlingInterpolationMode.PARABOLIC:
                        Handles.color = Color.yellow;
                        Handles.DrawDottedLine(from, inAnchor, 2);
                        Handles.color = Color.cyan;
                        Handles.DrawDottedLine(inAnchor, to, 2);
                        break;
                    default: // when using all 4 points
                        Handles.color = Color.yellow;
                        Handles.DrawDottedLine(from, inAnchor, 2);
                        Handles.color = Color.cyan;
                        Handles.DrawDottedLine(outAnchor, to, 2);
                        break;
                }

                // travelDistance += segmentLength[i];
                targetPosition[i] = to;
                targetOffset[i] = targetShift;
                startAnchor[i] = inAnchor;
                endAnchor[i] = outAnchor;
                from = to;
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(script, $"Modify {script.GetUdonTypeName()} Content");
                // only update the things that the handles adjust
                script.targetPosition = CopyArray(targetPosition);
                script.startAnchor = CopyArray(startAnchor);
                script.endAnchor = CopyArray(endAnchor);
                script._EDITOR_lastLocalObjPos = lastLocalObjPos;
            }
        }

        private Vector3 getSeamlessAnchorForSegment(int i)
        {
            if (i == 0) return Vector3.LerpUnclamped(script.transform.position, targetPosition[i], -1f);
            switch (interpolationMode[i - 1])
            {
                case FlingInterpolationMode.LINEAR: return i > 1 ? targetPosition[i - 2] : script.transform.position;
                case FlingInterpolationMode.BOUNCE: return startAnchor[i - 1];
                case FlingInterpolationMode.PARABOLIC: return startAnchor[i - 1];
                default: return endAnchor[i - 1]; // when using all 4 points
            }
        }
    }
}