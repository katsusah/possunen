using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using Jypeli.Effects;

/// @author Katja Ahonen
/// @version 11.12.2019
/// Tasohyppelypeli Possusen kiireinen päivä
/// <summary>
/// Tarkoituksena kerätä pikkupossut ja vältellä banaaninkuoria, joista menettää pisteitä.
/// Laava alkaa nousta kentän alaosasta ja siihen ei saa osua tai peli loppuu.
/// Seuraavaan kenttään siirrytään ovesta, kun kaikki possut on kerätty.
/// </summary>
public class PossusenKiireinenPaiva : PhysicsGame
{
    private const int KOKO = 20;        // Kentan yksittaisen ruudun koko TileMapissa
    private PlatformCharacter possunen; // Liikuteltava pelihahmo, jota pystytaan liikuttamaan kaikista taman luokan aliohjelmista.
    private PhysicsObject laava;        // Peliobjekti, jonka sijainnin muuttavaa aliohjelmaa kutsutaan ajastimen avulla.
    private readonly SoundEffect maaliAani = LoadSoundEffect("maali.wav"); // Ääni, jota käytetään pelissä.
    private readonly Image[] pikkuPossut = LoadImages("PikkuLila.png", "PikkuOkra.png", "PikkuOmppo.png", "PikkuPetro.png", "PikkuPinkki.png");
        // Pikkupossujen kuvatiedostot. Alkioiden määrä sama kuin possujen lukumäärä kentässä.
    private readonly Image[] kuvat = LoadImages("tausta.png", "laava2.png", "Possunen.png", "banana.png", "palikkaPitka.png", "palikkaLyhyt.png", "ovi.png");
        // Pelin muut kuvatiedostot.
    private readonly string[] kenttalista = { "kentta1.txt", "kentta2.txt", "kentta3.txt" }; // Listaus kenttien nimistä. Alkioiden määrä sama kuin pelin kenttien lukumäärä.
    private readonly EasyHighScore topLista = new EasyHighScore(); // Top 10 -lista.
    private IntMeter pisteLaskuri;      // Laskee kerätyt pisteet.
    private readonly Label pisteNaytto = new Label();   // Pistelaskurin näyttö.
    private readonly Label viesti = new Label();        // Viestinäyttö.
    private int kentta;                 // Muuttujaan lasketaan kenttien kulloinenkin järjestysnumero
    private int possuLkm;               // Kerättyjen pikkupossujen lukumäärä kentässä.

    /// <summary>
    /// Avataan alkuvalikko, josta voi käynnistää pelin, katsoa piste-ennätyksiä tai lopettaa pelin.
    /// </summary>
    public override void Begin()
    {
        LuoValikko();
        // TODO: silmukka ja funktio: https://tim.jyu.fi/answers/kurssit/tie/ohj1/2019s/demot/demo7?answerNumber=3&task=matriisiensumma&user=katsusah
    }


    /// <summary>
    /// Luodaan aloitusvalikko, josta voi aloittaa pelin, katsoa pistelistaa tai sulkea ohjelman.
    /// Asetetaan kentän numeroksi 1.
    /// </summary>
    public void LuoValikko()
    {
        ClearAll(); // Tyhjentää ruudun peliobjekteista pelin jäljiltä.
        pisteLaskuri = new IntMeter(0, int.MinValue, int.MaxValue);

        kentta = 1; // Valikosta siirrytään peliin aina 1. kenttään.

        // Luodaan aloitusvalikko ja asetetaan sille värit ja toiminnot:
        Level.Background.Color = Color.Wheat;
        MultiSelectWindow valikko = new MultiSelectWindow("Possusen kiireinen päivä", "Aloita peli", "Parhaat 10 tulosta", "Lopeta");
        Add(valikko);
        valikko.Color = Color.Wheat;
        valikko.SelectionColor = Color.LightGreen;
        valikko.AddItemHandler(0, SeuraavaKentta);  // Pelaaminen aloitetaan kutsumalla aliohjelmaa SeuraavaKentta.
        valikko.AddItemHandler(1, ParhaatPisteet);  // Pistelistaus saadaan kutsumalla ParhaatPisteet-aliohjelmaa.
        valikko.AddItemHandler(2, Exit);            // Ohjelman sulkeminen.
    }


    /// <summary>
    /// Aliohjelma näyttää pisteiden top 10 -listauksen.
    /// </summary>
    public void ParhaatPisteet()
    {
        topLista.Show(); // Näyttää pistelistauksen.
        topLista.HighScoreWindow.Closed += delegate { LuoValikko(); }; // Pistelistaus suljettaessa kutsutaan LuoValikko-aliohjelmaa.
    }


    /// <summary>
    /// Aloitetaan kentän luonti, asetetaan ohjaimet, ajastin ja pistelaskuri.
    /// </summary>
    public void SeuraavaKentta()
    {
        ClearAll();         // Tyhjätään ruutu edellisen kentän peliobjekteista ja ajastimista.
        AsetaOhjaimet();    // Asetetaan peliohjaimet.
        LuoTekstikentta(viesti, 0, Screen.Top - 50);
        viesti.Text = "";   // Tyhjennetään viestikenttä.
        LuoTekstikentta(pisteNaytto, Screen.Right - 80, Screen.Top - 40);  // Luodaan pistelaskuri.
        pisteNaytto.Title = "Pisteitä";
        pisteNaytto.BindTo(pisteLaskuri); // Liitetään pistenäyttö pistelaskuriin.

        Timer.CreateAndStart(2.5, LaavaNousee); // Asetetaan ajastin, joka vastaa laavan noususta.

        possuLkm = 0;       // Asetetaan kerättyjen pikkupossujen lukumääräksi 0 kentän alussa.
        Level.Width = 280;  // Kentän koko ja tausta määritetään, asetetaan kentälle reunat.
        Level.Height = 210;
        Level.Background.Image = kuvat[0];
        Level.Background.FitToLevel();
        Level.CreateBorders();
        Gravity = new Vector(0, -1000); // Asetetaan painovoima.

        TeeKentta(); // Kutsutaan TeeKentta-aliohjelmaa, joka luo varsinaisen kentän tasoineen.

        Vector sijaintiPossu = new Vector(0, Level.Bottom);
        LisaaPelaaja(sijaintiPossu, KOKO / 2, KOKO / 1.3); // Lisätään pelihahmo kentälle.

        LuoLaava(); // Luodaan laava.

        BoundingRectangle zoomi = new BoundingRectangle(0, Level.Bottom + 80, 280, 210); // Alue, johon zoomataan.
        Camera.ZoomTo(zoomi); // Zoomataan oikeaan kohtaan.
    }


    /// <summary>
    /// Luodaan pelikentta tekstitiedostosta TileMapin avulla. Kutsutaan aliohjelmia eri merkeille
    /// luotujen merkitysten perusteella.
    /// </summary>
    public void TeeKentta()
    {
        TileMap ruudut = TileMap.FromLevelAsset("kentta" + kentta + ".txt"); // Luodaan TileMap ruudut tekstitiedostosta, jonka nimi on muotoa "kentta" + kentän numero + "txt".

        ruudut.SetTileMethod('p', LuoPalikka, kuvat[4], 1.0, 0.25); // Luodaan pidempi taso kutsumalla aliohjelmaa LuoPalikka.
        ruudut.SetTileMethod('r', LuoPalikka, kuvat[5], 0.7, 0.25); // Luodaan lyhyempi taso.
        ruudut.SetTileMethod('o', LuoOvi);          // Luodaan ovi.
        ruudut.SetTileMethod('b', LuoBanaani);      // Luodaan banaaninkuoria.
        ruudut.SetTileMethod('.', LuoPossu);        // Luodaan pikkupossut.
        ruudut.Execute(KOKO, KOKO);                 // Luodaan kenttä käyttäen kokona vakiota KOKO = 20.
    }


    /// <summary>
    /// Aliohjelma luo peliobjektin pikkuPossu haluttuun sijaintiin halutun kokoisena.
    /// </summary>
    /// <param name="paikka">paikka, johon pikkuPossu sijoitetaan</param>
    /// <param name="leveys">pikkuPossun leveys</param>
    /// <param name="korkeus">pikkuPossun korkeus</param>
    public void LuoPossu(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject pikkuPossu = new PhysicsObject(leveys / 2, korkeus / 2); // pikkuPossun koko on KOKO / 2
        pikkuPossu.Position = paikka;   // pikkuPossun sijainti
        pikkuPossu.Image = pikkuPossut[RandomGen.NextInt(0, 5)]; // pikkuPossun kuva ladataan taulukosta satunnaisen ideksinumeron 0-4 kohdalta
        pikkuPossu.Tag = "pikkuPossu";  // annetaan pikkuPossulle tag
        Add(pikkuPossu);                // lisätään pikkuPossu kentälle
    }


    /// <summary>
    /// Aliohjelma, joka luo banaaninkuoret kentälle.
    /// </summary>
    /// <param name="paikka">paikka, johon banaaninkuoret sijoitetaan</param>
    /// <param name="leveys">banaaninkuorien leveys</param>
    /// <param name="korkeus">banaaninkuorien korkeus</param>
    public void LuoBanaani(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject banaani = new PhysicsObject(leveys / 3, korkeus / 6);
        banaani.Position = paikka;
        banaani.Image = kuvat[3];
        banaani.Tag = "banaani";
        Add(banaani);
    }


    /// <summary>
    /// Aliohjelma, joka luo tasot kentälle.
    /// </summary>
    /// <param name="paikka">paikka, johon taso sijoitetaan</param>
    /// <param name="leveys">tason leveys</param>
    /// <param name="korkeus">tason korkeus</param>
    /// <param name="kuva">kuva, joka sijoitetaan tason paikalle</param>
    /// <param name="leveysKerroin">kerroin, jolla leveys kerrotaan</param>
    /// <param name="korkeusKerroin">kerroin, jolla korkeus kerrotaan</param>
    public void LuoPalikka(Vector paikka, double leveys, double korkeus, Image kuva, double leveysKerroin, double korkeusKerroin)
    {
        PhysicsObject palikka = PhysicsObject.CreateStaticObject(leveys*leveysKerroin, korkeus*korkeusKerroin);
        palikka.Position = paikka;
        palikka.Image = kuva;
        Add(palikka);
    }


    /// <summary>
    /// Aliohjelma, joka luo oven kentälle.
    /// </summary>
    /// <param name="paikka">paikka, johon ovi sijoitetaan</param>
    /// <param name="leveys">oven leveys</param>
    /// <param name="korkeus">oven korkeus</param>
    public void LuoOvi(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject ovi = PhysicsObject.CreateStaticObject(leveys, korkeus * 1.2);
        ovi.Position = paikka;
        ovi.Image = kuvat[6];
        ovi.Shape = Shape.FromImage(ovi.Image); // oven ääriviivat luodaan kuvan mukaan
        ovi.Tag = "ovi";
        Add(ovi);
    }


    /// <summary>
    /// Luodaan pelihahmo.
    /// </summary>
    /// <param name="paikka">Paikka, johon hahmo luodaan kentällä.</param>
    /// <param name="leveys">Hahmon leveys.</param>
    /// <param name="korkeus">Hahmon korkeus.</param>
    public void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        possunen = new PlatformCharacter(leveys, korkeus, Shape.FromImage(kuvat[2]));
        possunen.Position = paikka;
        possunen.Image = kuvat[2];
        possunen.Mass = 4.0;
        AddCollisionHandler(possunen, "pikkuPossu", KeraaPossu); // Lisätään pelihahmolle törmäyskäsittelijät.
        AddCollisionHandler(possunen, "banaani", OsuBanaaniin);
        AddCollisionHandler(possunen, "ovi", MeneOvesta);
        AddCollisionHandler(possunen, "laava", OsuLaavaan);
        Add(possunen);
    }


    /// <summary>
    /// Asetetaan nappaintenkuuntelijat ja toiminnot nappaimille.
    /// </summary>
    private void AsetaOhjaimet()
    {
        const double hyppynopeus = 400.0;   // määritetään hyppy- ja kävelyvoimat
        const double nopeus = 100.0;

        // Nuolinäppäimillä liikutetaan pelihahmoa.
        Keyboard.Listen(Key.Up, ButtonState.Pressed, HyppaaPossu, "Hyppy ylös", hyppynopeus);
        Keyboard.Listen(Key.Left, ButtonState.Down, LiikuPossu, "Liikkuu vasemmalle", -nopeus);
        Keyboard.Listen(Key.Right, ButtonState.Down, LiikuPossu, "Liikkuu oikealle", nopeus);

        // Ohjeiden näyttämistä varten. Paina F1 pelin aikana.
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Ohjeet");

        // Esc pelin lopettamista varten.
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, Exit, "Poistu");
    }


    /// <summary>
    /// Aliohjelma luo tekstikentän, joka sijoitetaan peliruutuun.
    /// </summary>
    public void LuoTekstikentta(Label labelNimi, double x, double y)
    {
        labelNimi.X = x;                    // Näytön sijainti x-
        labelNimi.Y = y;                    // ja y-akselilla.
        labelNimi.TextColor = Color.Black;  // Tekstin väri.
        Add(labelNimi);
    }


    /// <summary>
    /// Aliohjelma liikuttaa pelihahmoa.
    /// </summary>
    /// <param name="nopeus">Nopeus, jolla hahmo liikkuu.</param>
    public void LiikuPossu(double nopeus)
    {
        possunen.Walk(nopeus);
    }


    /// <summary>
    /// Aliohjelma laittaa pelihahmon hyppäämään ylös.
    /// </summary>
    /// <param name="hyppynopeus">Hyppynopeus (voima)</param>
    public void HyppaaPossu(double hyppynopeus)
    {
        possunen.Jump(hyppynopeus);
    }


    /// <summary>
    /// Tormayskasittelija, kun pelihahmo osuu pikkupossuun.
    /// </summary>
    /// <param name="possunen">Pelihahmo</param>
    /// <param name="pikkuPossu">Kerattava objekti, pikkupossu</param>
    public void KeraaPossu(PhysicsObject possunen, PhysicsObject pikkuPossu)
    {
        maaliAani.Play();
        viesti.Text = "Pelastit pikkupossun! \n+500 pistettä!";
        possuLkm++; // Lisätään kerättyjen possujen lukumäärään 1.
        pikkuPossu.Destroy(); // Kun pikkuPossu on kerätty, se häviää ruudusta.
        pisteLaskuri.Value += 500; // Keratysta possusta saa +500 pistetta.
    }


    /// <summary>
    /// Törmäyskäsittelijä, kun pelihahmo osuu banaaninkuoreen.
    /// </summary>
    /// <param name="possunen">Pelihahmo</param>
    /// <param name="banaani">Objekti, johon osutaan</param>
    public void OsuBanaaniin(PhysicsObject possunen, PhysicsObject banaani)
    {
        maaliAani.Play();
        viesti.Text = "Osuit banaaninkuoreen! \n-300 pistettä!";
        banaani.Destroy();
        pisteLaskuri.Value -= 300; // Banaaninkuoresta menettaa 300 pistetta.
    }


    /// <summary>
    /// Törmäyskäsittelijä, kun pelihahmo osuu laavaan. Peli päättyy laavaan osumiseen.
    /// </summary>
    /// <param name="possunen">Pelihahmo</param>
    /// <param name="laava">Objekti, johon osutaan</param>
    public void OsuLaavaan(PhysicsObject possunen, PhysicsObject laava)
    {
        maaliAani.Play();
        viesti.Text = "Osuit laavaan, possusta tuli pekonia. \nParempi onni ensi kerralla!";
        Smoke savu = new Smoke();           // Luodaan pelihahmon tilalle savutehoste.
        savu.Position = possunen.Position;  // Savun sijainti on sama kuin pelihahmolla.
        savu.MaxScale = 1;                  // Savun koko.
        Add(savu);
        possunen.Destroy();                 // Poistetaan pelihahmo kentältä.
        topLista.EnterAndShow(pisteLaskuri.Value); // Avataan top 10 -pistelista.
        topLista.HighScoreWindow.Closed += delegate { Timer.CreateAndStart(3, LuoValikko); };
        // Siirrytään takaisin valikkoon, kun pistelistaus suljetaan.
    }


    /// <summary>
    /// Törmäyskäsittelijä, kun osutaan oveen.
    /// Ovesta kuljetaan seuraavaan kenttään, kun kaikki pikkupossut on kerätty.
    /// Peli päättyy oveen, kun kaikki possut kaikista kentistä on kerätty.
    /// </summary>
    /// <param name="possunen">Pelihahmo</param>
    /// <param name="ovi">Objekti, johon osutaan</param>
    public void MeneOvesta(PhysicsObject possunen, PhysicsObject ovi) // TODO: animoitu ovi
    {
        if (possuLkm == pikkuPossut.Length) // Tarkastetaan, etta kaikki possut on keratty.
                                            // Possuja on joka kentässä saman verran kuin pikkuPossut-taulukossa alkioita,
                                            // joskaan niin ei välttämättä olisi pakko olla, koska taukosta otetaan vain satunnaisesti jossain indeksissä oleva kuvatiedosto.
        {
            maaliAani.Play();
            Gravity = new Vector(0, -10);
            viesti.Text = "Läpäisit kentän! \n+5000 pistettä!";
            pisteLaskuri.Value += 5000;         // Kentan lapaisemalla saa +5000 pistetta.
            kentta++;                           // Kun kenttä on läpäisty, lisätään kenttien järjestysnumeroon 1.
            if (kentta <= kenttalista.Length) Timer.CreateAndStart(1, SeuraavaKentta); // Siirrytään seuraavaan kenttään, mikäli kenttänumero on enintään kenttien lukumäärä.
            else
            {
                ClearTimers(); // pysäytetään ajastimet, jottei laava nouse enää
                Gravity = new Vector (0, -10); // muutetaan painovoimaa, ettei possu liukastu laavaan
                viesti.Text = "Läpäisit kaikki kentät! \nPossut pääsivät turvaan!";
                topLista.EnterAndShow(pisteLaskuri.Value); // Jos kaikki kentät on läpäisty, siirrytään pistelistaukseen.
                topLista.HighScoreWindow.Closed += delegate { Timer.CreateAndStart(3, LuoValikko); }; // Siirrytään takaisin alkuvalikkoon.
            }
        }
        else viesti.Text = "Et ole vielä pelastanut \nkaikkia possuja!"; // Jos kaikkia possuja ei ole kentästä kerätty, tulee oveen osuttaessa tämä viesti.
    }


    /// <summary>
    /// Luodaan laava, joka on kentan alussa piilossa alhaalla.
    /// </summary>
    public void LuoLaava()  // TODO: parempi laava, jonka päällä ei voi kävellä
    {
        laava = PhysicsObject.CreateStaticObject(290, 220, Shape.FromImage(kuvat[1])); // laavan ääriviivat muodostetaan kuvan ääriviivojen mukaan
        laava.Image = kuvat[1];
        laava.Tag = "laava";
        laava.X = 0;
        laava.Y = Level.Bottom - 137; // laavan sijainti pelikentän alussa
        Add(laava);
    }


    /// <summary>
    /// Laava nousee aina aliohjelmaa kutsuttaessa y-akselilla niin monta pykälää kuin on kentän järjestysnumeron suuruus.
    /// Laavan nousunopeus siis lisääntyy kenttien kasvaessa.
    /// Kun laavan keskipiste on tasolla Level.Bottom-130 - 127, naytetaan varoitus.
    /// </summary>
    public void LaavaNousee()
    {
        laava.Y += kentta;
        if (laava.Y > Level.Bottom - 135 && laava.Y < Level.Bottom -130)
            viesti.Text = "VARO, LAAVA NOUSEE!!!";
    }
}