using System;
using System.Collections.Generic;
using System.Linq;
using LinkShot.Core;
using UnityEngine;

namespace LinkShot.Network
{
    /// <summary>
    /// Core.GameActionと、match_actions.payload(jsonb)としてSupabaseへ送るJSON文字列との相互変換(ARCHITECTURE.md 4章)。
    /// UnityのJsonUtilityはNullable&lt;T&gt;を扱えないため、各optionalフィールドに"hasXxx"の対フラグを用意する。
    /// </summary>
    public static class NetworkActionCodec
    {
        [Serializable]
        public class Payload
        {
            public string actionType;

            public int player;
            public string cardId;

            public int defaultWallCell;
            public int[] disposableWallCells;

            public int position;

            public string outcome;
            public string[] hitZones;

            public bool hasWallTarget;
            public int wallTargetCellIndex;
            public bool hasWallDestination;
            public int wallDestinationCellIndex;
            public bool hasBouncePosition;
            public float bounceX;
            public float bounceY;
            public bool hasWideGateZone;
            public string wideGateZone;
            public bool hasStolenWallOwner;
            public int stolenWallCardOwnerPlayer;
        }

        public static string Encode(GameAction action)
        {
            return JsonUtility.ToJson(ToPayload(action));
        }

        public static GameAction Decode(string json)
        {
            return DecodeFromPayload(JsonUtility.FromJson<Payload>(json));
        }

        /// <summary>PostgRESTのレスポンスで既にオブジェクトとしてパース済みのPayloadから復元する場合はこちら。</summary>
        public static GameAction DecodeFromPayload(Payload payload)
        {
            return ToAction(payload);
        }

        private static Payload ToPayload(GameAction action)
        {
            switch (action)
            {
                case SetCardAction a:
                    return new Payload { actionType = nameof(SetCardAction), player = a.Player, cardId = a.CardId };

                case PlaceWallsAction a:
                    return new Payload
                    {
                        actionType = nameof(PlaceWallsAction),
                        defaultWallCell = a.DefaultWallCell,
                        disposableWallCells = a.DisposableWallCells.ToArray(),
                    };

                case RollPositionAction:
                    return new Payload { actionType = nameof(RollPositionAction) };

                case ChoosePositionAction a:
                    return new Payload { actionType = nameof(ChoosePositionAction), position = a.Position };

                case RerollAction:
                    return new Payload { actionType = nameof(RerollAction) };

                case ConfirmPositionAction:
                    return new Payload { actionType = nameof(ConfirmPositionAction) };

                case ResolveEffectAction a:
                    return EncodeResolveEffect(a);

                case SubmitShotResultAction a:
                    return new Payload
                    {
                        actionType = nameof(SubmitShotResultAction),
                        outcome = a.Outcome.ToString(),
                        hitZones = a.HitZones.Select(z => z.ToString()).ToArray(),
                    };

                case ProceedAction:
                    return new Payload { actionType = nameof(ProceedAction) };

                default:
                    throw new ArgumentException($"未対応のアクション: {action.GetType()}");
            }
        }

        private static Payload EncodeResolveEffect(ResolveEffectAction a)
        {
            var payload = new Payload { actionType = nameof(ResolveEffectAction) };
            EffectChoice choice = a.Choice;

            if (choice.WallTargetCellIndex.HasValue)
            {
                payload.hasWallTarget = true;
                payload.wallTargetCellIndex = choice.WallTargetCellIndex.Value;
            }

            if (choice.WallDestinationCellIndex.HasValue)
            {
                payload.hasWallDestination = true;
                payload.wallDestinationCellIndex = choice.WallDestinationCellIndex.Value;
            }

            if (choice.BouncePosition.HasValue)
            {
                payload.hasBouncePosition = true;
                payload.bounceX = choice.BouncePosition.Value.X;
                payload.bounceY = choice.BouncePosition.Value.Y;
            }

            if (choice.WideGateZone.HasValue)
            {
                payload.hasWideGateZone = true;
                payload.wideGateZone = choice.WideGateZone.Value.ToString();
            }

            if (choice.StolenWallCardOwnerPlayer.HasValue)
            {
                payload.hasStolenWallOwner = true;
                payload.stolenWallCardOwnerPlayer = choice.StolenWallCardOwnerPlayer.Value;
            }

            return payload;
        }

        private static GameAction ToAction(Payload payload)
        {
            switch (payload.actionType)
            {
                case nameof(SetCardAction):
                    return new SetCardAction(payload.player, payload.cardId);

                case nameof(PlaceWallsAction):
                    return new PlaceWallsAction(payload.defaultWallCell, payload.disposableWallCells ?? Array.Empty<int>());

                case nameof(RollPositionAction):
                    return new RollPositionAction();

                case nameof(ChoosePositionAction):
                    return new ChoosePositionAction(payload.position);

                case nameof(RerollAction):
                    return new RerollAction();

                case nameof(ConfirmPositionAction):
                    return new ConfirmPositionAction();

                case nameof(ResolveEffectAction):
                    return new ResolveEffectAction(DecodeEffectChoice(payload));

                case nameof(SubmitShotResultAction):
                    var outcome = (ShotOutcomeKind)Enum.Parse(typeof(ShotOutcomeKind), payload.outcome);
                    IReadOnlyList<TargetZoneId> hitZones = (payload.hitZones ?? Array.Empty<string>())
                        .Select(z => (TargetZoneId)Enum.Parse(typeof(TargetZoneId), z))
                        .ToList();
                    return new SubmitShotResultAction(outcome, hitZones);

                case nameof(ProceedAction):
                    return new ProceedAction();

                default:
                    throw new ArgumentException($"未対応のactionType: {payload.actionType}");
            }
        }

        private static EffectChoice DecodeEffectChoice(Payload payload)
        {
            var choice = new EffectChoice();

            if (payload.hasWallTarget)
            {
                choice.WallTargetCellIndex = payload.wallTargetCellIndex;
            }

            if (payload.hasWallDestination)
            {
                choice.WallDestinationCellIndex = payload.wallDestinationCellIndex;
            }

            if (payload.hasBouncePosition)
            {
                choice.BouncePosition = new Vec2(payload.bounceX, payload.bounceY);
            }

            if (payload.hasWideGateZone)
            {
                choice.WideGateZone = (TargetZoneId)Enum.Parse(typeof(TargetZoneId), payload.wideGateZone);
            }

            if (payload.hasStolenWallOwner)
            {
                choice.StolenWallCardOwnerPlayer = payload.stolenWallCardOwnerPlayer;
            }

            return choice;
        }
    }
}
