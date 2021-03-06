﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KKSFramework.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;
using HexaPuzzle;
using KKSFramework.ResourcesLoad;
using UniRx.Async;
using Zenject;

namespace KKSFramework
{
    public class GamePageView : PageViewBase
    {
        #region Fields & Property

        /// <summary>
        /// All of line element objects.
        /// </summary>
        public LineElement[] lineElements;


        public GameObject touchPreventionObj;

        /// <summary>
        /// Container of destroyed puzzles.
        /// </summary>
        public Transform puzzleContainer;

        /// <summary>
        /// Parents of generated puzzles.
        /// </summary>
        public Transform puzzleParents;

        /// <summary>
        /// All lands dictionary, distinguished by position.
        /// </summary>
        public Dictionary<int, LineElement> AllLineElements { get; private set; } =
            new Dictionary<int, LineElement> ();


#pragma warning disable CS0649

        [Inject]
        private PuzzleViewmodel _puzzleViewmodel;

#pragma warning restore CS0649

        /// <summary>
        /// generated puzzles
        /// </summary>
        private readonly Stack<PuzzleElement> _puzzleElements = new Stack<PuzzleElement> ();

        #endregion


        protected override async UniTask OnPush (object pushValue = null)
        {
            touchPreventionObj.SetActive (false);
            _puzzleViewmodel.StartStage (EndPuzzleMovement);

            AllLineElements = _puzzleViewmodel.AllLineModels
                .ToDictionary (x => x.Key, x => lineElements[x.Key].SetLineModel (x.Value));

            for (var c = 0; c < AllLineElements.Count; c++)
            {
                for (var r = 0; r < AllLineElements[c].landElements.Length; r++)
                {
                    if (AllLineElements[c].LineModel.LandDatas[r].landTypes == LandTypes.Hide)
                        continue;

                    var element = AllLineElements[c].GetLandElement (r);
                    element.gameObject.SetActive (true);
                    var resObj = ResourcesLoadHelper.GetResources<PuzzleElement> (ResourceRoleType._Prefab,
                        ResourcesType.Element, "PuzzleElement");
                    var prefab =
                        ProjectContext.Instance.Container.InstantiatePrefabForComponent<PuzzleElement> (resObj);
                    prefab.transform.SetParent (puzzleParents);
                    prefab.transform.SetInstantiateTransform ();

                    _puzzleElements.Push (prefab);
                }
            }

            foreach (var puzzleModel in _puzzleViewmodel.AllPuzzleModels)
            {
                var poppedElement = _puzzleElements.Pop ();
                poppedElement.SetPuzzleView (_puzzleViewmodel, this);
                poppedElement.SetModel (puzzleModel);
                poppedElement.transform.position = AllLineElements[puzzleModel.PositionModel.Column]
                    .GetLandElement (puzzleModel.PositionModel.Row).transform.position;
            }

            await WaitDisplayPuzzles ().ToUniTask ();
            await base.OnPush (pushValue);
        }


        /// <summary>
        /// End game.
        /// </summary>
        public void EndGame ()
        {
        }


        /// <summary>
        /// Contain removed puzzle. 
        /// </summary>
        public void ContainPuzzle (PuzzleElement element)
        {
            element.gameObject.SetActive (false);
            element.transform.position = puzzleContainer.transform.position;
        }


        private IEnumerator WaitDisplayPuzzles ()
        {
            yield return new WaitForSeconds (2.5f);
        }


        /// <summary>
        /// 퍼즐 위치가 변경됨.
        /// </summary>
        public async Task ChangePuzzlePosition (PuzzleModel originPuzzle, PuzzleModel targetPuzzle, float angle)
        {
            touchPreventionObj.SetActive (true);
            _puzzleViewmodel.ChangeControlMode (true, _puzzleViewmodel.GetPuzzleDirection (angle),
                new List<PuzzleModel> {originPuzzle, targetPuzzle});

            await _puzzleViewmodel.ConvertPuzzlePosition (originPuzzle, targetPuzzle);

            // 위치 이동이 가능함.
            if (await _puzzleViewmodel.PuzzleCheckFlow ())
            {
                return;
            }

            await _puzzleViewmodel.ConvertPuzzlePosition (originPuzzle, targetPuzzle);

            if (!_puzzleViewmodel.CheckMovablePuzzleState ())
            {
                // 재배열 팝업.
                Debug.Log ("재배열 해야함.");
            }

            EndPuzzleMovement ();
        }


        /// <summary>
        /// 모든 퍼즐 이동이 마무리됨.
        /// </summary>
        public void EndPuzzleMovement ()
        {
            touchPreventionObj.SetActive (false);
        }


        public void Restart ()
        {
            SceneManager.LoadScene (0);
        }

        public void CompleteAlignedPuzzles (PuzzleModel puzzleModel)
        {
            _puzzleViewmodel.CompleteAlignedPuzzles (puzzleModel);
        }


        public bool IsContainLineElement (int column)
        {
            return AllLineElements.ContainsKey (column);
        }


        public bool IsContainLandElement (PositionModel positionModel)
        {
            return IsContainLineElement (positionModel.Column) &&
                   AllLineElements[positionModel.Column].IsContainRow (positionModel.Row);
        }


        public LandElement GetLandElement (PositionModel positionModel)
        {
            return IsContainLandElement (positionModel)
                ? AllLineElements[positionModel.Column].GetLandElement (positionModel.Row)
                : null;
        }
    }
}