using System;
using UnityEngine;


public class CustomSwitchController : MonoBehaviour
{
	public Vector3i blockPos;
	public int cIdx;
	public BlockEntityData ebcd;
	public TileEntity parentTileEntity;
	public TileEntityPowerSource parentPowerSource;
	private bool showDebugLog = true;
	
	public void DebugMsg(string msg)
	{
		if(showDebugLog)
		{
			Debug.Log(msg);
		}
	}

}