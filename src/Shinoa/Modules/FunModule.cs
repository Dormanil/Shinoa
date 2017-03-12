﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Shinoa.Modules
{
    public class FunModule : ModuleBase<SocketCommandContext>
    {
        static readonly string[] jojoMemes = {
            "Rohan’s holding a pen!",
            "We’re going to ignore the law.",
            "Are you hiding somewhere in my body? Somewhere in my pants?",
            "I see… Guess it could be useful for fixing joint and back pains… Becoming a snail, that is.",
            "Jotaro! That cow is out of control!",
            "Who cares about the goddamn cow, Polnareff? I’m bleeding over here!",
            "Yes. We are friends. *Punches cute girl straight in the mouth* FRIENDS OF JUSTICE",
            "Now the, while Jojo is taking off his pants, ",
            "His nails, his flesh, even his bones have become as limp and flexible as a ‘condom’ …",
            "You there, Chinese… You should know… Where’s the shop that sells Oriental poisons?",
            "I’m truly happy to have met this snake.",
            "Saying that I resemble a Michelangelo statue would be putting it lightly.",
            "Okay, Master! Let’s kill-da-ho! Beeeeetch!",
            "You were drinking my IV fluid?",
            "S-H-I-T",
            "I never really had time to look at my enemy’s crotch or anything----",
            "“Tell him to go eat shit, Johnny.”  “Tell him yourself.”",
            "EAT SHIT, ASSHOLE! FALL OFF YOUR HORSE!",
            "Although I’d like to talk a bit about global climate change and environmental destruction right now… I’ll instead be using this opportunity to talk about myself.",
            "Go ahead, Meestur Juhstur.",
            "OH! MY! GOOOD!",
            "MY NAILS WERE JUST SHOT OUT FROM MY LAME FEEEEET!!",
            "TURN THE CAR INTO A PLANT, GIORNO!",
            "Eggs? Eggs? What in the world…",
            "An invisible baby in clear water?!",
            "Gramps will explain everything when you’re in the turtle.",
            "Maybe your own neighbor is a Rock Human.",
            "Shit! There are frogs on my 800 dollar pants!",
            "If I stare t his ‘Ass’ he whole time… I can’t win!",
            "She’s got some 「CASH」... money.",
            "See how much my nails have grown? Can anyone stop their nails from growing?",
            "KAKYOIN!! DID YOU LAY THIS EGG?!",
            "Hey, look, Jojo. Flamingos in flight.",
            "“Don’t worry. I have confidence. I’m pretty good at video games…” – Kakyoin, about to face off against a nigh-invincible vampire",
            "GOODBYE, JOJO! UOOAAAAH!",
            "Do not worry, my lily white friends.",
            "I am the nicest man in the world. I have girlfriends everywhere.",
            "Bathroom disasters are Polnareff’s thing! This is not in line with my character!",
            "I won. Part 3 is over. And who’s going to replace me as main character? You?",
            "(Polnareff, after Kakyoin elbows him in the face) Thank you, Kakyoin.",
            "My fourth wish is to not listen to your wishes.",
            "Are you really trying to shoot me? I like you.",
            "“Help me out, Jotaro.” “Do it yourself, old man.”",
            "It’s for certain! As certain as when you piss in a strong wind, it’ll get on your pants!",
            "“We’ll use my last plan.” “Please tell me you don’t mean That Plan.” “IT’S TIME TO RUN AWAY, SMOKEY!” “I KNEW IT!”",
            "OH NOOOOO",
            "SONODABITCH",
            "“You can’t pay back what you owe with money. ORA ORA ORA ORA ORA ORA ORA!... Here’s your receipt.” – Jotaro, beating the shit out of a civilian",
            "Niiiiiiice",
            "ORA ORA ORA ORA ORA ORA",
            "MUDA MUDA MUDA MUDA MUDA MUDA",
            "When i was a kid, I saw Mona Lisa from my garmmar school art book... the first time I saw her, with hands on her knee.... how do I say this... I had a boner..",
            "I feel you! I feel you deeply! Your feeling I can feel deeply!",
            "I’m not moaning because I want to!",
            "“Let me punch this ‘spaghetti’ and reduce it to its original parts!” – Josuke Higashikata, while punching spaghetti",
            "WRYYYYYYYYYYYYYY",
            "Could a monkey fight a man?",
            "How many breads have you eaten in your life?",
            "This feels like a picnic",
            "What\na\nbeautiful\nD U W A NG",
            "You can’t go around showing people your asshole.",
            "That ‘spaghetti’ looks suspicious!",
            "Even Speedwagon is afraid!",
            "What the fuck did you just say about my hair?",
            "I turned your gun into a banana. Treat it as a last meal.",
            "A surfer? That’s not a real job.",
            "Who are you? WHO IN FACE ARE YOU?",
            "I made the “Bus” look like my ‘Dad’…",
            "Is he a 「Stand User」? Or does he just say those idiotic things naturally?",
            "This GPS is an 「Ally」. I was saved by this GPS.",
            "ROAD ROLLER DA!!!",
            "A sponge. And another sponge.",
            "I put the steel ball in the can. How I got it in there is a secret",
            "Again… Again… Lick… Lick my face… My horse…",
            "Was that damn joke supposed to be funny? BECAUSE I FORGOT TO LAUGH!",
            "…. ‘Ear’ ….",
            "Oh… ! Hey fishes! Come over here! TOUCH MY BODY!",
            "We used masks to turn these horses into vampires.",
            "I’ll gladly turn into a piece of paper.",
            "I really am a hot, beautiful guy.",
            "Rerorerorerorerorerorero",
            "The culprit’s name is ‘Sports Max’.",
            "It’s too late! Kiler Queen has already touched that 100 yen coin!",
            "I can make spirit photos that show far off lands and places! But I have to smash a 30 thousand yen camera to do it.",
            " Banana…. !?!?!?!?",
            "KONO\nDIO\nDA",
            "“Cartoonists secretly own real handguns, and fire them in their basements.” – The creator of JJBA, on why he became a cartoonist",
            "The World Over Heaven already hit that turtle.",
            "‘POWER’!! ‘GLORY’ !! ‘HAPPINESS’ ! ‘Culture’, ‘Law’, ‘Money’! ‘ The hearts of the people’! I HAVE TAKEN THE FIRST NAPKIN!",
            "I…. INSIDE THE URINAL!?",
            "He ate my left leg…",
            "I’m concentrating all of Morioh City’s energy on my chili pepper.",
            "THROW THE TURTLE, NARANCIA!",
            "I didn’t expect the Third Reich to show up.",
            "Nude selfies~",
            "It’s been a while since mom got naked in front of dad.",
            "One of you three is on your period, right?",
            "I AM THE FUCKING STRONG",
            "You OK reatard? I am wood.",
            "This feels like a picnic."};

        public static Color MODULE_COLOR = new Color(63, 81, 181);

        [Command("pick"), Alias("choose")]
        public async Task Pick([Remainder]string args)
        {
            var choices = args.Split(new[] { " or " }, StringSplitOptions.RemoveEmptyEntries);
            var choice = choices[new Random().Next(choices.Length)].Trim();

            var embed = new EmbedBuilder()
            .WithTitle($"I choose '{choice}'.")
            .WithColor(MODULE_COLOR);

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("roll"), Alias("rolldice")]
        public async Task RollDice(string arg)
        {
            var rng = new Random();
            var multiplier = int.Parse(arg.Split('d')[0]);
            var dieSize = int.Parse(arg.Split('d')[1]);
            var total = 0;

            if (multiplier > 100)
            {
                await ReplyAsync("Please stick to reasonable amounts of dice.");
                return;
            }

            var rollsString = "";
            foreach (int i in 1.To(multiplier))
            {
                int roll = rng.Next(dieSize) + 1;
                rollsString += $"{roll}, ";
                total += roll;
            }

            var embed = new EmbedBuilder()
                .AddField(f => f.WithName("Total").WithValue(total.ToString()))
                .AddField(f => f.WithName("Rolls").WithValue(rollsString.Trim(' ', ',')))
                .WithColor(MODULE_COLOR);

           await ReplyAsync("", embed: embed);
        }

        [Command("jojo"), Alias("duwang")]
        public async Task JojoMeme()
        {
            var random = new Random();
            var meme = jojoMemes[random.Next(jojoMemes.Length)];
            await ReplyAsync(meme);
        }
    }
}
