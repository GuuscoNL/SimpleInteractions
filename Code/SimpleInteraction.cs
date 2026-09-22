using Sandbox;
using Sandbox.Utility;
using Sandbox.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleInteractions
{

	/// <summary>
	/// Simple interaction component
	/// </summary>
	[Icon( "touch_app" )]
	[Title( "Simple Interaction" )]
	public class SimpleInteraction : Component
	{
		[Property, Title( "Interaction Name" )]
		public string InteractionString { get; set; } = "Interact";

		[Property]
		public float InteractionDistance { get; set; } = 120f;

		[Property, ToggleGroup( "InteractionHold" )]
		public bool InteractionHold { get; set; } = false;

		[Property, Group( "InteractionHold" )]
		public float InteractionHoldDuration { get; set; } = 0.5f;


		/// <summary>
		/// If not set, will try to find a collider on the same GameObject.
		/// </summary>
		[Property, Title( "Override collider" )]
		public Collider Collider { get; set; }

		private GameObject CurrentPanel = null;

		private TimeSince HoldTime = 0;
		private bool Holding = false;
		private bool HoldingInteractionHappened = false;

		static protected GameObject InteractionPanelPrefab;

		protected override void OnStart()
		{
			InteractionPanelPrefab = GameObject.GetPrefab( "InteractionsPanel.prefab" );

			Assert.True( InteractionPanelPrefab.IsValid(), $"No InteractionPanel prefab found for {this.GameObject.Name}!" );

			if ( !Collider.IsValid() )
			{

				Collider = this.GameObject.GetComponent<Collider>();

				Assert.True( Collider.IsValid(), $"No collider found for {this.GameObject.Name}!" );
			}
			this.GameObject.Tags.Add( "Interact" );
		}

		protected override void OnUpdate()
		{
			if ( !IsAllowed() ) // TODO: Not every frame pls
			{
				// Reset everything just in case
				Holding = false;
				HoldingInteractionHappened = false;

				// Delete the Interaction panel otherwise it would just float there...
				if ( CurrentPanel.IsValid() )
				{
					InteractionPanel panel = CurrentPanel.GetComponent<InteractionPanel>();
					if ( panel.IsValid() )
					{
						_ = DeletePanel();
					}
				}
				return;
			}
			// Log.Info( "============ NEW FRAME ============ " );

			Ray ray = Scene.Camera.GameObject.Transform.World.ForwardRay;

			var traces = Scene.Trace.Ray( ray, InteractionDistance )
			.WithoutTags( "IgnoreInteract" )
			.HitTriggers()
			.RunAll();

			// Gizmo.Draw.Line(tr.StartPosition, tr.EndPosition);

			if ( traces.Count() <= 0 )
			{
				_ = DeletePanel();

				// Force repressing use in case you looked away while holding down.
				HoldingInteractionHappened = true;
				return;
			}

			foreach ( var tr in traces )
			{

				Collider HitCollider = tr.Shape.Collider as Collider;

				// If it's a trigger and it doesn't have the interact tag, skip it.
				// We can see through it.
				if ( HitCollider.IsTrigger && !HitCollider.GameObject.Tags.Has( "Interact" ) )
				{
					continue;
				}
				else if ( !HitCollider.IsTrigger && !HitCollider.GameObject.Tags.Has( "Interact" ) )
				{
					// A non interaction is blocking the interaction.
					_ = DeletePanel();

					// Force repressing use in case you looked away while holding down.
					HoldingInteractionHappened = true;
					break;
				}

				//	Trigger interact
				//	X Trigger not interact
				//	Coll  interact
				//	X Coll not interact

				// Log.Info( tr.GameObject.Name );
				if ( HitCollider == Collider )
				{
					// Log.Info( $"HIT: {tr.GameObject.Name}" );
					// Log.Info( $"HIT: {HitCollider}" );
					// Log.Info( $"HIT: {Collider}" );

					Vector3 offset = Vector3.Zero;

					if ( HitCollider is BoxCollider )
					{
						offset = (HitCollider as BoxCollider).Center;
					}
					else if ( HitCollider is SphereCollider )
					{
						offset = new Vector3( (HitCollider as SphereCollider).Center );
					}

					Vector3 pos = new Vector3( offset.x, offset.y, -offset.z );
					OnHover( HitCollider.GameObject.WorldPosition - pos );
					break;
				}
				else
				{

					if ( !HitCollider.IsTrigger && tr.GameObject.Tags.Has( "Interact" ) )
					{
						_ = DeletePanel();

						// Force repressing use in case you looked away while holding down.
						HoldingInteractionHappened = true;

						break;
					}
				}
			}
		}

		private void OnHover( Vector3 pos )
		{
			// Log.Info( "OnHover" );
			if ( !CurrentPanel.IsValid() )
			{
				CurrentPanel = InteractionPanelPrefab.Clone();
			}

			CurrentPanel.WorldPosition = pos;

			// Flip the panel to face the camera
			Rotation camRotation = Scene.Camera.WorldRotation;

			Angles ang = camRotation.Angles();
			ang.roll += 180;
			ang.pitch += 180;
			Rotation rot = ang.ToRotation();
			CurrentPanel.WorldRotation = rot;

			InteractionPanel panel = CurrentPanel.GetComponent<InteractionPanel>();
			panel.InteractionString = InteractionString;
			panel.IsHoldInteraction = InteractionHold;
			panel.ProgressionHold = 0;



			if ( !InteractionHold )
			{
				if ( Input.Pressed( "use" ) )
				{
					_ = panel.TriggerInteractAnimation();
					OnInteract();
				}
				return;
			}


			if ( !Input.Down( "use" ) )
			{
				Holding = false;
				HoldingInteractionHappened = false;
				return;
			}

			// Interaction already happened. Player needs to release and press again.
			if ( HoldingInteractionHappened )
			{
				return;
			}

			if ( Holding )
			{
				panel.ProgressionHold = Easing.QuadraticInOut( HoldTime / InteractionHoldDuration );
				if ( HoldTime >= InteractionHoldDuration )
				{
					HoldingInteractionHappened = true;
					OnInteract();
				}
			}
			else
			{
				// Started holding.
				Holding = true;
				HoldTime = 0;
				_ = panel.TriggerInteractAnimation();
			}
		}

		async private Task DeletePanel()
		{
			if ( !CurrentPanel.IsValid() ) return;

			CurrentPanel.GetComponent<PanelComponent>().Panel.Delete();
			await Task.DelaySeconds( 0.1f );
			CurrentPanel.Destroy();
		}

		[Rpc.Broadcast]
		protected virtual void OnInteract()
		{
			Log.Error( $"Interaction not implemented for {this.GameObject.Name}!" );
		}

		protected virtual bool IsAllowed()
		{
			return true;
		}

	}

}