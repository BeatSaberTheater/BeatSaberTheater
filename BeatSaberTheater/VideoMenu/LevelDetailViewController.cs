using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu;

[UsedImplicitly]
public class LevelDetailViewController
{
	private readonly LevelDetailComponent _root = null!;
	private readonly StandardLevelDetailViewController? _standardLevelDetailViewController;

	internal event Action? ButtonPressedAction;

	// ReSharper disable Unity.InefficientPropertyAccess
	internal LevelDetailViewController()
	{
		_standardLevelDetailViewController =
			Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().LastOrDefault();
		if (_standardLevelDetailViewController == null) return;

		var levelDetail = _standardLevelDetailViewController.transform.Find("LevelDetail");
		if (levelDetail == null)
		{
			_standardLevelDetailViewController = null;
			return;
		}

		_root = new LevelDetailComponent(levelDetail.gameObject.transform);
		_root.ButtonPressed += () => ButtonPressedAction?.Invoke();
		SetActive(false);

		// _buttonUnderline = _button.transform.Find("Underline").gameObject.GetComponent<Image>();

		//Clone background from level difficulty selection
		var beatmapDifficulty = levelDetail.Find("BeatmapDifficulty");
		var beatmapCharacteristic = levelDetail.Find("BeatmapCharacteristic");
		var actionButtons = levelDetail.Find("ActionButtons");
		var levelDetailBackground = beatmapDifficulty.Find("BG");
		if (beatmapDifficulty == null || beatmapCharacteristic == null || actionButtons == null ||
		    levelDetailBackground == null)
		{
			_standardLevelDetailViewController = null;
			return;
		}

		var characteristicTransform = beatmapCharacteristic.GetComponent<RectTransform>();
		var difficultyTransform = beatmapDifficulty.GetComponent<RectTransform>();
		var actionButtonTransform = actionButtons.GetComponent<RectTransform>();

		//The difference between characteristic and difficulty transforms. Using this would make it equal size to those
		var offsetMinYDifference = difficultyTransform.offsetMin.y +
		                           (difficultyTransform.offsetMin.y - characteristicTransform.offsetMin.y);
		//The maximum it can be without overlapping with the action buttons
		var offsetMinYMax = actionButtonTransform.offsetMin.y + actionButtonTransform.sizeDelta.y;
		//We take whichever is larger to make best use of the available space
		var offsetMinY = Math.Max(offsetMinYDifference, offsetMinYMax);

		var offsetMin = new Vector2(difficultyTransform.offsetMin.x, offsetMinY);
		var offsetMax = new Vector2(difficultyTransform.offsetMax.x,
			difficultyTransform.offsetMax.y + (difficultyTransform.offsetMax.y - characteristicTransform.offsetMax.y));

		var rectTransform = _root.ContentTransform;
		rectTransform.anchorMin = difficultyTransform.anchorMin;
		rectTransform.anchorMax = difficultyTransform.anchorMax;
		rectTransform.offsetMin = offsetMin;
		rectTransform.offsetMax = offsetMax;
	}

	public void SetActive(bool active)
	{
		if (_standardLevelDetailViewController == null) return;

		_root.Content.SetActive(active);
	}

	public void SetText(string? label, string? button = null, Color? textColor = null, Color? underlineColor = null)
	{
		if (_standardLevelDetailViewController == null) return;

		_root.SetLabelText(label ?? "", textColor ?? Color.white, label != null);
		_root.SetButtonState(button ?? "", underlineColor ?? Color.clear, button != null);
	}

	public void RefreshContent()
	{
		if (_standardLevelDetailViewController != null)
			_standardLevelDetailViewController.RefreshContentLevelDetailView();
	}
}