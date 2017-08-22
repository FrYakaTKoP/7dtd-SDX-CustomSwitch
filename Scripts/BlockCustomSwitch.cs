using Audio;
using GUI_2;
using System;
using UnityEngine;

public class BlockCustomSwitch : BlockPowered
{	
    private bool showDebugLog = true;

	public void DebugMsg(string msg)
	{
		if(showDebugLog)
		{
            String msg2 = String.Concat("SDX BlockCustomSwitch.", msg);
			Debug.Log(msg2);
		}
	}
    
	private BlockActivationCommand[] MU = new BlockActivationCommand[]
	{
		new BlockActivationCommand("light", "electric_switch", false),
		new BlockActivationCommand("take", "hand", false)
	};
    
    // gets called onFocus of the switch 
	public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
	{
        //string msg = String.Concat("GetActivationText - _blockValue.meta &2 :", (_blockValue.meta & 2));
        //DebugMsg(msg);
        // (_blockValue.meta &2):
        // 0 : switch is Off , 1 : switch is On
    
		PlayerActionsLocal playerInput = ((EntityPlayerLocal)_entityFocusing).playerInput;
		string keybindString = UIUtils.GetKeybindString(playerInput.Activate, playerInput.PermanentActions.Activate);
        if ((_blockValue.meta & 2) != 0)
		{

			//return string.Format(Localization.Get("useSwitchLightOff", string.Empty), keybindString);
			return string.Format("cSwitchOff", keybindString);
		}
		//return string.Format(Localization.Get("useSwitchLightOn", string.Empty), keybindString);
        return string.Format("cSwitchOn", keybindString);
    }
    
	// calls when player use the block
	public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
	{
        DebugMsg("OnBlockActivated");
		if (_indexInBlockActivationCommands != 0)
		{
			if (_indexInBlockActivationCommands != 1)
			{
				return false;
			}
			base.TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
			return true;
		}
		else
		{
			if (!(_world.GetTileEntity(_cIdx, _blockPos) is TileEntityPoweredTrigger))
			{
				return false;
			}
			this.XR(_world, _cIdx, _blockPos, _blockValue, true);
			return true;
		}
	}

	// 
	private bool XR(WorldBase worldBase, int num, Vector3i vector3i, BlockValue blockValue, bool flagOnBlockActivated)
	{
        DebugMsg("XR");
        if(flagOnBlockActivated == null)
        {
            flagOnBlockActivated = false;
        }
		ChunkCluster chunkCluster = worldBase.ChunkClusters[num];
		if (chunkCluster == null)
		{
			return false;
		}
		if (chunkCluster.GetChunkSync(World.toChunkXZ(vector3i.x), World.toChunkY(vector3i.y), World.toChunkXZ(vector3i.z)) == null)
		{
			return false;
		}
				
		bool hasSecondInput = HasActivePower(worldBase, num, vector3i);
		
		bool flag2 = (blockValue.meta & 1) != 0; // seems to be BlockIsPowered
		bool flag3 = (blockValue.meta & 2) != 0; // seems to be SwitchIsOn 
		
		string flag2msg = flag2 ? "true" : "false";
		string flag3msg = flag3 ? "true" : "false"; 
		string msg = "XR - BlockValue.meta &2=";
		msg = String.Concat(msg, flag2msg);
		msg = String.Concat(msg, " &3=");
		msg = String.Concat(msg, flag3msg);
		// notworking f***
		// string msg = String.Format("XR - BlockValue.meta &2=", flag2msg, " &3=", flag3msg);	
		// string msg = String.Format("XR - BlockValue.meta &2=", flag2 ? "true" : "false", " &3=", flag3 ? "true" : "false");
		DebugMsg(msg);
		
		if (flagOnBlockActivated)
		{
			flag3 = !flag3;
			blockValue.meta = (byte)(((int)blockValue.meta & -3) | ((!flag3) ? 0 : 2));
			blockValue.meta = (byte)(((int)blockValue.meta & -2) | ((!flag2) ? 0 : 1));
			worldBase.SetBlockRPC(num, vector3i, blockValue);
			if (flag3)
			{
				Manager.BroadcastPlay(vector3i.ToVector3(), "switch_up");
			}
			else
			{
				Manager.BroadcastPlay(vector3i.ToVector3(), "switch_down");
			}
		}
		if (Steam.Network.IsServer)
		{
			TileEntityPoweredTrigger tileEntityPoweredTrigger = worldBase.GetTileEntity(num, vector3i) as TileEntityPoweredTrigger;
			if (tileEntityPoweredTrigger != null)
			{
				tileEntityPoweredTrigger.IsTriggered = flag3;
			}
		}
		BlockEntityData blockEntity = ((World)worldBase).ChunkClusters[num].GetBlockEntity(vector3i);
		if (blockEntity != null && blockEntity.transform != null && blockEntity.transform.gameObject != null)
		{
			Renderer[] componentsInChildren = blockEntity.transform.gameObject.GetComponentsInChildren<Renderer>();
			if (componentsInChildren != null)
			{
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					if (componentsInChildren[i].material != componentsInChildren[i].sharedMaterial)
					{
						componentsInChildren[i].material = new Material(componentsInChildren[i].sharedMaterial);
					}
					Color tempColor;
					if (flag2)
					{
						// switch is powered
						if(flag3)
						{
							// switch is On
							if(hasSecondInput == true){
								// switch has 2nd input
								tempColor = Color.blue;
							}
							else
							{
								tempColor = Color.green;								
							}
						}
						else
							// switch is Off
							if(hasSecondInput == true){
								// switch has 2nd input
								tempColor = Color.yellow;
							}
							else
							{
								tempColor = Color.red;								
							}							
						}
					else
					{							
						// switch has no power
						if(hasSecondInput == true){
							// test wise a "there is 2nd input" even no power on the switch
							tempColor = new Color(1f, 0.92f, 0.016f, 0.6f); //Color.yellow;
						}
						else
						{
							tempColor = Color.red;								
						}	
					}
					componentsInChildren[i].material.SetColor("_EmissionColor", tempColor);
					componentsInChildren[i].sharedMaterial = componentsInChildren[i].material;
				}
			}
		}
		return true;
	}
	
		
	static Vector3i[] PowerInputLocations(Vector3i _blockPos)
	{
		int inputSpace = 1;
		Vector3i inputPosA = _blockPos;
		Vector3i inputPosB = _blockPos;
		Vector3i inputPosC = _blockPos;
		Vector3i inputPosD = _blockPos;
		
		inputPosA.y = _blockPos.y+inputSpace;
		inputPosB.y = _blockPos.y-inputSpace;
		inputPosC.x = _blockPos.x+inputSpace;
		inputPosD.x = _blockPos.x-inputSpace;
		
		Vector3i[] array = new Vector3i[4];
		array[0] = inputPosA;
		array[1] = inputPosB;
		array[2] = inputPosC;
		array[3] = inputPosD;
		return array;
		
	}
	
	public static bool HasActivePower(WorldBase _world, int _cIdx, Vector3i _blockPos)
	{
		Vector3i[] locations = PowerInputLocations(_blockPos);
		foreach (Vector3i vector in locations)
		{
			BlockValue inputBlockValue = _world.GetBlock(vector);
			Type inputBlockType = Block.list[inputBlockValue.type].GetType();
			if(inputBlockType == typeof(BlockPowered))
			{
				TileEntityPowered tileEntityPowered = (TileEntityPowered)_world.GetTileEntity(_cIdx, vector);
				if (tileEntityPowered != null)
				{
					if(tileEntityPowered.IsPowered)
					{
						return true;
					}
				}
			}
		}
		return false;
	}
	

	public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx, BlockValue _blockValue, BlockEntityData _ebcd)
	{
        DebugMsg("OnBlockEntityTransformAfterActivated");
		base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
		bool flag = (_blockValue.meta & 1) != 0;
		bool flag2 = (_blockValue.meta & 2) != 0;
		if (_ebcd != null && _ebcd.transform != null && _ebcd.transform.gameObject != null)
		{
			Renderer[] componentsInChildren = _ebcd.transform.gameObject.GetComponentsInChildren<Renderer>();
			if (componentsInChildren != null)
			{
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					if (componentsInChildren[i].material != componentsInChildren[i].sharedMaterial)
					{
						componentsInChildren[i].material = new Material(componentsInChildren[i].sharedMaterial);
					}
					if (flag)
					{
						componentsInChildren[i].material.SetColor("_EmissionColor", (!flag2) ? Color.red : Color.green);
					}
					else
					{
						componentsInChildren[i].material.SetColor("_EmissionColor", Color.black);
					}
					componentsInChildren[i].sharedMaterial = componentsInChildren[i].material;
				}
			}
		}
	}

	public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue, BlockValue _newBlockValue)
	{
        DebugMsg("OnBlockValueChanged");
		base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
		this.XR(_world, _clrIdx, _blockPos, _newBlockValue, false);
		BlockEntityData blockEntity = ((World)_world).ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
		this.HR(blockEntity, BlockSwitch.IsSwitchOn(_newBlockValue.meta), _newBlockValue);
	}

	public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
	{
		bool flag = _world.IsMyLandProtectedBlock(_blockPos, _world.GetGameManager().GetPersistentLocalPlayer());
		this.MU[0].enabled = true;
		this.MU[1].enabled = (flag && this.TakeDelay > 0f);
		return this.MU;
	}

	public override bool ActivateBlock(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, bool isOn, bool isPowered)
	{
        DebugMsg("ActivateBlock");
		_blockValue.meta = (byte)(((int)_blockValue.meta & -3) | ((!isOn) ? 0 : 2));
		_blockValue.meta = (byte)(((int)_blockValue.meta & -2) | ((!isPowered) ? 0 : 1));
		_world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
		this.XR(_world, _cIdx, _blockPos, _blockValue, false);
		return true;
	}

	public override TileEntityPowered CreateTileEntity(Chunk chunk)
	{
		return new TileEntityPoweredTrigger(chunk);
	}

	public static bool IsSwitchOn(byte _metadata)
	{
		return (_metadata & 2) != 0;
	}

	private void HR(BlockEntityData blockEntityData, bool value, BlockValue blockValue)
	{
        DebugMsg("HR");
		Animator[] componentsInChildren;
		if (blockEntityData != null && blockEntityData.bHasTransform && (componentsInChildren = blockEntityData.transform.GetComponentsInChildren<Animator>()) != null)
		{
			Animator[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				Animator animator = array[i];
				animator.SetBool("SwitchActivated", value);
				animator.SetTrigger("SwitchTrigger");
			}
		}
	}

	public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
	{
        DebugMsg("ForceAnimationState");
		Animator[] componentsInChildren;
		if (_ebcd != null && _ebcd.bHasTransform && (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) != null)
		{
			bool flag = BlockSwitch.IsSwitchOn(_blockValue.meta);
			Animator[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				Animator animator = array[i];
				animator.SetBool("SwitchActivated", flag);
				if (flag)
				{
					animator.CrossFade("SwitchOnStatic", 0f);
				}
				else
				{
					animator.CrossFade("SwitchOffStatic", 0f);
				}
			}
		}
	}
}
