﻿using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace RedistHeat
{
	public class AirNet
	{
		private float temperature;
		private static int lastId;

		public List<CompAir> nodes = new List<CompAir>();

		public float Temperature
		{
			get { return temperature; }
			set { temperature = Mathf.Clamp(value, -270, 2000); }
		}

		public int NetId
		{
			get;
			private set;
		}
		public AirNet()
		{
			NetId = checked(AirNet.lastId++);
			temperature = GenTemperature.OutdoorTemp;
		}
		public AirNet(IEnumerable<CompAir> newNodes)
			: this()
		{
			foreach (var current in newNodes)
			{
				RegisterNode(current);
			}
		}
		private AirNet(CompAir newNode)
			: this(new List<CompAir> { newNode })
		{
		}
		public void RegisterNode(CompAir node)
		{
			if (node.connectedNet == this)
			{
				Log.Warning("Tried to register " + node + " on net it's already on!");
			}
			else
			{
				if (node.connectedNet != null)
					node.connectedNet.DeregisterNode(node);
				else
				{
					nodes.Add(node);
					node.connectedNet = this;
				}
			}
		}
		// ReSharper disable once MemberCanBePrivate.Global
		public void DeregisterNode(CompAir node)
		{
			nodes.Remove(node);
			node.connectedNet = null;
		}
		public void PushHeat(float e)
		{
			if(nodes.Count == 1)
				Temperature += e / nodes.Count;
			else
				Temperature += e * 2 / nodes.Count;
		}
		public void MergeIntoNet(AirNet newNet)
		{
			foreach (var current in new List<CompAir>(nodes))
			{
				DeregisterNode(current);
				newNet.RegisterNode(current);
			}
		}
		public void SplitNetAt(CompAir node)
		{
			//Must check inside for underneath pipe
			foreach (var current in GenAdj.CardinalDirectionsAndInside)
			{
				var compAir = AirNetGrid.AirNodeAt(node.Position + current);
				if (compAir == null || compAir.connectedNet != this)
					continue;
				AirNet.ContiguousNodes(compAir);
			}
		}
		private static AirNet ContiguousNodes(CompAir root)
		{
			var connectedNet = root.connectedNet;
			connectedNet.DeregisterNode(root);
			var airNet = new AirNet(root);

			//Should check inside?
			foreach (var current in GenAdj.CardinalDirectionsAndInside)
			{
				var compAir = AirNetGrid.AirNodeAt(root.Position + current);
				if (compAir != null && compAir.connectedNet == connectedNet)
				{
					AirNet.ContiguousNodes(compAir).MergeIntoNet(airNet);
				}
			}
			return airNet;
		}
	}
}