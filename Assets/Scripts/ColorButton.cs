using UnityEngine;
using System.Collections;

public class ColorButton : MonoBehaviour
{
	public Color color;
	public UnityEngine.UI.Image imageToColor;

	public bool selected;

	private Animator anim;

	public void SetColor(Color col)
	{
		imageToColor.color = color = col;
	}

	public void OnClick()
	{
		FindObjectOfType<Customization_EditMenu>().NewColorChosen(this);
	}

	void Update()
	{
		if (anim.GetBool("Chosen") != selected)
			anim.SetBool("Chosen", selected);
	}

	void Awake()
	{
		anim = GetComponent<Animator>();
	}
}
