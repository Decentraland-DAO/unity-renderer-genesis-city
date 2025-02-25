﻿using DCL;
using DCL.Helpers;
using DCL.Components;
using DCL.Models;
using NUnit.Framework;
using System.Collections;
using DCL.Components.Video.Plugin;
using UnityEngine;
using UnityEngine.TestTools;
using DCL.Controllers;
using DCL.Interface;
using DCL.SettingsCommon;
using AudioSettings = DCL.SettingsCommon.AudioSettings;

namespace Tests
{
    public class VideoTests : IntegrationTestSuite_Legacy
    {
        protected override IEnumerator SetUp()
        {
            yield return base.SetUp();
            DCLVideoTexture.isTest = true;
        }

        protected override IEnumerator TearDown()
        {
            sceneController.enabled = true;
            return base.TearDown();
        }

        [UnityTest]
        public IEnumerator VideoTextureIsCreatedCorrectly()
        {
            DCLVideoTexture videoTexture = CreateDCLVideoTexture(scene, "it-wont-load-during-test");
            yield return videoTexture.routine;
            Assert.IsTrue(videoTexture.attachedMaterials.Count == 0, "DCLVideoTexture started with attachedMaterials != 0");
        }

        [UnityTest]
        public IEnumerator MessageIsSentWhenVideoPlays()
        {
            var id = CreateDCLVideoClip(scene, "http://it-wont-load-during-test").id;
            DCLVideoTexture.Model model = new DCLVideoTexture.Model()
            {
                videoClipId = id,
                playing = true,
                seek = 10
            };
            var component = CreateDCLVideoTextureWithCustomTextureModel(scene, model);

            var expectedEvent = new WebInterface.SendVideoProgressEvent()
            {
                sceneId = scene.sceneData.id,
                componentId = component.id,
                videoLength = 0,
                videoTextureId = id,
                currentOffset = 0,
                status = (int)VideoState.ERROR // status is always error when not on WebGL
            };

            var json = JsonUtility.ToJson(expectedEvent);
            var wasEventSent = false;
            yield return TestHelpers.WaitForMessageFromEngine("VideoProgressEvent", json,
                () => { },
                () => wasEventSent = true);

            Assert.IsTrue(wasEventSent, $"Event of type {expectedEvent.GetType()} was not sent or its incorrect.");
        }

        [UnityTest]
        public IEnumerator MessageIsSentWhenVideoStops()
        {
            var id = CreateDCLVideoClip(scene, "http://it-wont-load-during-test").id;
            DCLVideoTexture.Model model = new DCLVideoTexture.Model()
            {
                videoClipId = id,
                playing = false
            };
            var component = CreateDCLVideoTextureWithCustomTextureModel(scene, model);

            var expectedEvent = new WebInterface.SendVideoProgressEvent()
            {
                sceneId = scene.sceneData.id,
                componentId = component.id,
                videoLength = 0,
                videoTextureId = id,
                currentOffset = 0,
                status = (int)VideoState.ERROR
            };

            var json = JsonUtility.ToJson(expectedEvent);
            var wasEventSent = false;
            yield return TestHelpers.WaitForMessageFromEngine("VideoProgressEvent", json,
                () => { },
                () => wasEventSent = true);

            Assert.IsTrue(wasEventSent, $"Event of type {expectedEvent.GetType()} was not sent or its incorrect.");
        }
        [UnityTest]
        public IEnumerator MessageIsSentWhenVideoIsUpdatedAfterTime()
        {
            var id = CreateDCLVideoClip(scene, "http://it-wont-load-during-test").id;
            DCLVideoTexture.Model model = new DCLVideoTexture.Model()
            {
                videoClipId = id,
                playing = true
            };
            var component = CreateDCLVideoTextureWithCustomTextureModel(scene, model);

            var expectedEvent = new WebInterface.SendVideoProgressEvent()
            {
                sceneId = scene.sceneData.id,
                componentId = component.id,
                videoLength = 0,
                videoTextureId = id,
                currentOffset = 0,
                status = (int)VideoState.ERROR
            };
            var json = JsonUtility.ToJson(expectedEvent);
            var wasEventSent = false;
            yield return TestHelpers.WaitForMessageFromEngine("VideoProgressEvent", json,
                () => { },
                () => wasEventSent = true);

            Assert.IsTrue(wasEventSent, $"Event of type {expectedEvent.GetType()} was not sent or its incorrect.");
        }

        [UnityTest]
        public IEnumerator VideoTextureReplaceOtherTextureCorrectly()
        {
            DCLVideoTexture videoTexture = CreateDCLVideoTexture(scene, "it-wont-load-during-test");
            yield return videoTexture.routine;
            Assert.IsTrue(videoTexture.attachedMaterials.Count == 0, "DCLVideoTexture started with attachedMaterials != 0");

            DCLTexture dclTexture = TestHelpers.CreateDCLTexture(
                scene,
                TestAssetsUtils.GetPath() + "/Images/atlas.png",
                DCLTexture.BabylonWrapMode.CLAMP,
                FilterMode.Bilinear);

            yield return dclTexture.routine;

            BasicMaterial mat = TestHelpers.SharedComponentCreate<BasicMaterial, BasicMaterial.Model>
            (scene, CLASS_ID.BASIC_MATERIAL,
                new BasicMaterial.Model
                {
                    texture = dclTexture.id
                });

            yield return mat.routine;

            yield return TestHelpers.SharedComponentUpdate<BasicMaterial, BasicMaterial.Model>(mat, new BasicMaterial.Model() { texture = videoTexture.id });

            Assert.IsTrue(videoTexture.attachedMaterials.Count == 1, $"did DCLVideoTexture attach to material? {videoTexture.attachedMaterials.Count} expected 1");
        }

        [UnityTest]
        public IEnumerator VideoTextureIsAttachedAndDetachedCorrectly()
        {
            DCLVideoTexture videoTexture = CreateDCLVideoTexture(scene, "it-wont-load-during-test");
            yield return videoTexture.routine;
            Assert.IsTrue(videoTexture.attachedMaterials.Count == 0, "DCLVideoTexture started with attachedMaterials != 0");

            BasicMaterial mat2 = TestHelpers.SharedComponentCreate<BasicMaterial, BasicMaterial.Model>
            (scene, CLASS_ID.BASIC_MATERIAL,
                new BasicMaterial.Model
                {
                    texture = videoTexture.id
                });

            yield return mat2.routine;

            Assert.IsTrue(videoTexture.attachedMaterials.Count == 1, $"did DCLVideoTexture attach to material? {videoTexture.attachedMaterials.Count} expected 1");

            // TEST: DCLVideoTexture detach on material disposed
            mat2.Dispose();
            Assert.IsTrue(videoTexture.attachedMaterials.Count == 0, $"did DCLVideoTexture detach from material? {videoTexture.attachedMaterials.Count} expected 0");

            videoTexture.Dispose();

            yield return null;
            Assert.IsTrue(videoTexture.texture == null, "DCLVideoTexture didn't dispose correctly?");
        }

        [UnityTest]
        public IEnumerator VideoTextureVisibleStateIsSetCorrectlyWhenAddedToAMaterialNotAttachedToShape()
        {
            DCLVideoTexture videoTexture = CreateDCLVideoTexture(scene, "it-wont-load-during-test");
            yield return videoTexture.routine;

            var ent1 = TestHelpers.CreateSceneEntity(scene);
            BasicMaterial ent1Mat = TestHelpers.SharedComponentCreate<BasicMaterial, BasicMaterial.Model>(scene, CLASS_ID.BASIC_MATERIAL, new BasicMaterial.Model() { texture = videoTexture.id });
            TestHelpers.SharedComponentAttach(ent1Mat, ent1);
            yield return ent1Mat.routine;

            Assert.IsTrue(!videoTexture.isVisible, "DCLVideoTexture should not be visible without a shape");
        }

        [UnityTest]
        public IEnumerator VideoTextureVisibleStateIsSetCorrectly()
        {
            DCLVideoTexture videoTexture = CreateDCLVideoTexture(scene, "it-wont-load-during-test");
            yield return videoTexture.routine;

            var ent1 = TestHelpers.CreateSceneEntity(scene);
            BasicMaterial ent1Mat = TestHelpers.SharedComponentCreate<BasicMaterial, BasicMaterial.Model>(scene, CLASS_ID.BASIC_MATERIAL, new BasicMaterial.Model() { texture = videoTexture.id });
            TestHelpers.SharedComponentAttach(ent1Mat, ent1);
            yield return ent1Mat.routine;

            BoxShape ent1Shape = TestHelpers.SharedComponentCreate<BoxShape, BoxShape.Model>(scene, CLASS_ID.BOX_SHAPE, new BoxShape.Model());
            yield return ent1Shape.routine;

            TestHelpers.SharedComponentAttach(ent1Shape, ent1);
            yield return new WaitForAllMessagesProcessed();
            Assert.IsTrue(videoTexture.isVisible, "DCLVideoTexture should be visible");

            yield return TestHelpers.SharedComponentUpdate<BoxShape, BoxShape.Model>(ent1Shape, new BoxShape.Model() { visible = false });
            yield return new WaitForAllMessagesProcessed();

            Assert.IsTrue(!videoTexture.isVisible, "DCLVideoTexture should not be visible ");
        }

        [UnityTest]
        public IEnumerator VideoTextureVisibleStateIsSetCorrectlyWhenAddedToAlreadyAttachedMaterial()
        {
            DCLVideoTexture videoTexture = CreateDCLVideoTexture(scene, "it-wont-load-during-test");
            yield return videoTexture.routine;

            var ent1 = TestHelpers.CreateSceneEntity(scene);
            BasicMaterial ent1Mat = TestHelpers.SharedComponentCreate<BasicMaterial, BasicMaterial.Model>(scene, CLASS_ID.BASIC_MATERIAL, new BasicMaterial.Model());
            TestHelpers.SharedComponentAttach(ent1Mat, ent1);
            yield return ent1Mat.routine;

            BoxShape ent1Shape = TestHelpers.SharedComponentCreate<BoxShape, BoxShape.Model>(scene, CLASS_ID.BOX_SHAPE, new BoxShape.Model());
            yield return ent1Shape.routine;

            TestHelpers.SharedComponentAttach(ent1Shape, ent1);

            yield return TestHelpers.SharedComponentUpdate<BasicMaterial, BasicMaterial.Model>(ent1Mat, new BasicMaterial.Model() { texture = videoTexture.id });
            yield return new WaitForAllMessagesProcessed();
            Assert.IsTrue(videoTexture.isVisible, "DCLVideoTexture should be visible");

            yield return TestHelpers.SharedComponentUpdate<BoxShape, BoxShape.Model>(ent1Shape, new BoxShape.Model() { visible = false });
            yield return new WaitForAllMessagesProcessed();

            Assert.IsTrue(!videoTexture.isVisible, "DCLVideoTexture should not be visible ");
        }

        [UnityTest]
        public IEnumerator VideoTextureVisibleStateIsSetCorrectlyWhenEntityIsRemoved()
        {
            DCLVideoTexture videoTexture = CreateDCLVideoTexture(scene, "it-wont-load-during-test");
            yield return videoTexture.routine;

            var ent1 = TestHelpers.CreateSceneEntity(scene);
            BasicMaterial ent1Mat = TestHelpers.SharedComponentCreate<BasicMaterial, BasicMaterial.Model>(scene, CLASS_ID.BASIC_MATERIAL, new BasicMaterial.Model() { texture = videoTexture.id });
            TestHelpers.SharedComponentAttach(ent1Mat, ent1);
            yield return ent1Mat.routine;

            BoxShape ent1Shape = TestHelpers.SharedComponentCreate<BoxShape, BoxShape.Model>(scene, CLASS_ID.BOX_SHAPE, new BoxShape.Model());
            yield return ent1Shape.routine;

            TestHelpers.SharedComponentAttach(ent1Shape, ent1);
            yield return new WaitForAllMessagesProcessed();
            Assert.IsTrue(videoTexture.isVisible, "DCLVideoTexture should be visible");

            scene.RemoveEntity(ent1.entityId, true);
            yield return new WaitForAllMessagesProcessed();

            Assert.IsTrue(!videoTexture.isVisible, "DCLVideoTexture should not be visible ");
        }

        [UnityTest]
        public IEnumerator VolumeWhenVideoCreatedWithNoUserInScene()
        {
            // We disable SceneController monobehaviour to avoid its current scene id update
            sceneController.enabled = false;

            // Set current scene as a different one
            CommonScriptableObjects.sceneID.Set("unexistent-scene");

            DCLVideoTexture videoTexture = CreateDCLVideoTexture(scene, "it-wont-load-during-test");
            yield return videoTexture.routine;

            var ent1 = TestHelpers.CreateSceneEntity(scene);
            BasicMaterial ent1Mat = TestHelpers.SharedComponentCreate<BasicMaterial, BasicMaterial.Model>(scene, CLASS_ID.BASIC_MATERIAL, new BasicMaterial.Model() { texture = videoTexture.id });
            TestHelpers.SharedComponentAttach(ent1Mat, ent1);
            yield return ent1Mat.routine;

            BoxShape ent1Shape = TestHelpers.SharedComponentCreate<BoxShape, BoxShape.Model>(scene, CLASS_ID.BOX_SHAPE, new BoxShape.Model());
            yield return ent1Shape.routine;

            TestHelpers.SharedComponentAttach(ent1Shape, ent1);
            yield return new WaitForAllMessagesProcessed();

            // Check the volume
            Assert.AreEqual(0f, videoTexture.texturePlayer.volume);
        }

        [UnityTest]
        public IEnumerator UpdateTexturePlayerVolumeWhenAudioSettingsChange()
        {
            var id = CreateDCLVideoClip(scene, "http://it-wont-load-during-test").id;
            DCLVideoTexture.Model model = new DCLVideoTexture.Model()
            {
                videoClipId = id,
                playing = true,
                volume = 1
            };
            var component = CreateDCLVideoTextureWithCustomTextureModel(scene, model);

            yield return null;

            AreAproximatedlyEqual(1f, component.texturePlayer.volume);

            AudioSettings settings = Settings.i.audioSettings.Data;
            settings.sceneSFXVolume = 0.5f;
            Settings.i.audioSettings.Apply(settings);
            
            var expectedVolume = Utils.ToVolumeCurve(0.5f);
            AreAproximatedlyEqual(expectedVolume, component.texturePlayer.volume);
            
            settings.sceneSFXVolume = 1f;
            Settings.i.audioSettings.Apply(settings);
            
            AreAproximatedlyEqual(1f, component.texturePlayer.volume);

        }

        private void AreAproximatedlyEqual(float expected, float current)
        {
            Assert.IsTrue(Mathf.Abs(current - expected) < Mathf.Epsilon, $"expected {expected} but was {current}");
        }

        [UnityTest]
        public IEnumerator VolumeWhenVideoCreatedWithUserInScene()
        {
            // We disable SceneController monobehaviour to avoid its current scene id update
            sceneController.enabled = false;

            // Set current scene with this scene's id
            CommonScriptableObjects.sceneID.Set(scene.sceneData.id);

            DCLVideoTexture videoTexture = CreateDCLVideoTexture(scene, "it-wont-load-during-test");
            yield return videoTexture.routine;

            var ent1 = TestHelpers.CreateSceneEntity(scene);
            BasicMaterial ent1Mat = TestHelpers.SharedComponentCreate<BasicMaterial, BasicMaterial.Model>(scene, CLASS_ID.BASIC_MATERIAL, new BasicMaterial.Model() { texture = videoTexture.id });
            TestHelpers.SharedComponentAttach(ent1Mat, ent1);
            yield return ent1Mat.routine;

            BoxShape ent1Shape = TestHelpers.SharedComponentCreate<BoxShape, BoxShape.Model>(scene, CLASS_ID.BOX_SHAPE, new BoxShape.Model());
            yield return ent1Shape.routine;

            TestHelpers.SharedComponentAttach(ent1Shape, ent1);
            yield return new WaitForAllMessagesProcessed();

            // Check the volume
            Assert.AreEqual(videoTexture.GetVolume(), videoTexture.texturePlayer.volume);
        }

        [UnityTest]
        public IEnumerator VolumeIsMutedWhenUserLeavesScene()
        {
            // We disable SceneController monobehaviour to avoid its current scene id update
            sceneController.enabled = false;

            // Set current scene with this scene's id
            CommonScriptableObjects.sceneID.Set(scene.sceneData.id);
            yield return null;

            DCLVideoTexture videoTexture = CreateDCLVideoTexture(scene, "it-wont-load-during-test");
            yield return videoTexture.routine;

            var ent1 = TestHelpers.CreateSceneEntity(scene);
            BasicMaterial ent1Mat = TestHelpers.SharedComponentCreate<BasicMaterial, BasicMaterial.Model>(scene, CLASS_ID.BASIC_MATERIAL, new BasicMaterial.Model() { texture = videoTexture.id });
            TestHelpers.SharedComponentAttach(ent1Mat, ent1);
            yield return ent1Mat.routine;

            BoxShape ent1Shape = TestHelpers.SharedComponentCreate<BoxShape, BoxShape.Model>(scene, CLASS_ID.BOX_SHAPE, new BoxShape.Model());
            yield return ent1Shape.routine;

            TestHelpers.SharedComponentAttach(ent1Shape, ent1);
            yield return new WaitForAllMessagesProcessed();

            // Set current scene as a different one
            CommonScriptableObjects.sceneID.Set("unexistent-scene");

            // to force the video player to update its volume
            CommonScriptableObjects.playerCoords.Set(new Vector2Int(666, 666));

            yield return null;

            // Check the volume
            Assert.AreEqual(0f, videoTexture.texturePlayer.volume);
        }

        [UnityTest]
        public IEnumerator VolumeIsUnmutedWhenUserEntersScene()
        {
            // We disable SceneController monobehaviour to avoid its current scene id update
            sceneController.enabled = false;

            // Set current scene as a different one
            CommonScriptableObjects.sceneID.Set("unexistent-scene");

            DCLVideoTexture videoTexture = CreateDCLVideoTexture(scene, "it-wont-load-during-test");
            yield return videoTexture.routine;

            var ent1 = TestHelpers.CreateSceneEntity(scene);
            BasicMaterial ent1Mat = TestHelpers.SharedComponentCreate<BasicMaterial, BasicMaterial.Model>(scene, CLASS_ID.BASIC_MATERIAL, new BasicMaterial.Model() { texture = videoTexture.id });
            TestHelpers.SharedComponentAttach(ent1Mat, ent1);
            yield return ent1Mat.routine;

            BoxShape ent1Shape = TestHelpers.SharedComponentCreate<BoxShape, BoxShape.Model>(scene, CLASS_ID.BOX_SHAPE, new BoxShape.Model());
            yield return ent1Shape.routine;

            TestHelpers.SharedComponentAttach(ent1Shape, ent1);
            yield return new WaitForAllMessagesProcessed();

            // Set current scene with this scene's id
            CommonScriptableObjects.sceneID.Set(scene.sceneData.id);

            // to force the video player to update its volume
            CommonScriptableObjects.playerCoords.Set(new Vector2Int(666, 666));

            yield return null;

            // Check the volume
            Assert.AreEqual(videoTexture.GetVolume(), videoTexture.texturePlayer.volume);
        }

        static DCLVideoClip CreateDCLVideoClip(ParcelScene scn, string url)
        {
            return TestHelpers.SharedComponentCreate<DCLVideoClip, DCLVideoClip.Model>
            (
                scn,
                DCL.Models.CLASS_ID.VIDEO_CLIP,
                new DCLVideoClip.Model
                {
                    url = url
                }
            );
        }

        static DCLVideoTexture CreateDCLVideoTexture(ParcelScene scn, DCLVideoClip clip)
        {
            return TestHelpers.SharedComponentCreate<DCLVideoTexture, DCLVideoTexture.Model>
            (
                scn,
                DCL.Models.CLASS_ID.VIDEO_TEXTURE,
                new DCLVideoTexture.Model
                {
                    videoClipId = clip.id
                }
            );
        }

        static DCLVideoTexture CreateDCLVideoTextureWithModel(ParcelScene scn, DCLVideoTexture.Model model)
        {
            return TestHelpers.SharedComponentCreate<DCLVideoTexture, DCLVideoTexture.Model>
            (
                scn,
                CLASS_ID.VIDEO_TEXTURE,
                model
            );
        }

        static DCLVideoTexture CreateDCLVideoTexture(ParcelScene scn, string url) { return CreateDCLVideoTexture(scn, CreateDCLVideoClip(scn, "http://" + url)); }
        static DCLVideoTexture CreateDCLVideoTextureWithCustomTextureModel(ParcelScene scn, DCLVideoTexture.Model model) { return CreateDCLVideoTextureWithModel(scn, model); }

    }
}